ТЗ (техническое задание) на решение “накликал в десктоп‑Avalonia → получил C# код headless‑теста”, с 4 артефактами: рекордер‑NuGet, тест‑хелперы‑NuGet, пример приложения, пример тестового проекта.

---

# ТЗ: Recorder → Codegen → Avalonia.Headless Tests

## 0. Цель

Сделать инструмент, который:

1. подключается к **десктопному AvaloniaUI приложению** как NuGet,
2. позволяет **в режиме записи** прокликать сценарий (клик, скролл, ввод текста, нажатия клавиш, наведение/считывание значений),
3. **генерирует воспроизводимый C# код** headless‑теста (Avalonia.Headless) и сохраняет его в файл,
4. предоставляет отдельные **тестовые хелперы** (DSL-обёртки), чтобы сгенерированный код был короткий, читабельный и стабильно отрабатывал.

---

## 1) Компонент №1: NuGet библиотека рекордера (для приложения)

### 1.1. Название и пакет

* Рабочее имя: `Avalonia.TestRecorder`
* Тип: .NET class library (NuGet)
* Целевая платформа: `net8.0` (опционально multi-target `net8.0`, `net7.0`)
* Совместимость: Avalonia 11.x (минимальная версия фиксируется в csproj)

### 1.2. Основные функции

**F1. Управление записью**

* Режимы:

  * `Off` (по умолчанию)
  * `Recording`
  * `Paused`
* Способы включения:

  * программно (API)
  * через переменную окружения (`AV_RECORDER=1`) и/или аргумент командной строки (`--record-tests`)
  * (опционально) через скрытое меню/команду в приложении “Start/Stop Recording”
* Горячие клавиши по умолчанию (настраиваемые):

  * `Ctrl+Shift+R` — Start/Stop
  * `Ctrl+Shift+P` — Pause/Resume
  * `Ctrl+Shift+S` — Save test code to file
  * `Ctrl+Shift+A` — “Capture Assert” по контролу под курсором / фокусу
  * `Esc` (опц.) — отменить текущую “операцию захвата” (например, выбор элемента)

**F2. Запись действий пользователя**
Записывать шаги с привязкой к “селектору” контрола:

* Клик (Left/Right/Double — минимум Left)
* Hover/Move (НЕ писать каждый move; только по явной команде или как часть assert-capture)
* Скролл колесом мыши (delta)
* Ввод текста (через `TextInput`, со склейкой в один шаг)
* Неформатный ввод (Enter/Tab/Backspace/Delete/Escape/стрелки) — как отдельные key‑шаги
* (Опционально, как расширение) Drag&Drop / selection / copy/paste

**F3. Стабильные селекторы**
Стратегия идентификации элемента (по приоритету):

1. `AutomationProperties.AutomationId` — основной корректный путь (обязателен для стабильности)
2. `Name` (fallback)
3. “Путь по дереву” (тип + индекс) — *крайний fallback* с предупреждением
4. Координаты — только если ничего не найдено, также с предупреждением

> В результирующем коде должны отмечаться “нестабильные шаги” (комментарием `// WARNING: fallback selector`).

**F4. Захват проверок (assert)**

* Команда “Capture Assert” должна:

  * определить целевой контрол (под курсором **или** текущий focus — настраиваемо),
  * прочитать “значение” через набор экстракторов,
  * записать шаг `Assert*`.
* Базовые экстракторы (из коробки):

  * `TextBox.Text`
  * `TextBlock.Text`
  * `ContentControl.Content?.ToString()`
  * `ToggleSwitch.IsChecked`, `CheckBox.IsChecked` (опц.)
  * визуальные/сервисные свойства (опц., конфиг): `IsVisible`, `IsEnabled`
* Поддержать расширяемость: интерфейс `IAssertValueExtractor`, чтобы приложение могло добавить свои правила (например, для кастомных контролов).

**F5. Генерация кода теста**

* Вывод:

  * C# файл с тестом (xUnit или NUnit — выбрать однозначно в MVP, второй флейвор можно как опцию)
  * опционально: JSON/YAML со “шагами” рядом (для диффа/перегенерации)
* Генерация:

  * класс теста `Recorded_<ScenarioName>_Tests`
  * метод `Scenario_<timestamp_or_name>()`
  * внутри: `var ui = new Ui(window);` + последовательность действий `ui.Click("...")`, `ui.TypeText("...", "...")` + asserts
* Шаблоны кода:

  * должны храниться в библиотеке (embedded resources) и позволять переопределение через `RecorderOptions.TemplateProvider`.

**F6. Сохранение в файл**

* Настройки:

  * папка вывода (дефолт: `%TEMP%/avalonia-recorded-tests` или рядом с exe в `./RecordedTests` в Debug)
  * соглашение об именах: `{AppName}.{ScenarioName}.{yyyyMMdd_HHmmss}.g.cs`
  * поведение при конфликте: AlwaysNew / Overwrite / Increment
* Логи:

  * писать в `ILogger` (Microsoft.Extensions.Logging) + опционально файл рядом.

**F7. UX (минимальный, но полезный)**

* Небольшой overlay/Toast в углу окна:

  * статус: Recording/Paused
  * текущий файл сохранения
  * кол-во шагов
* (Опционально) мини‑панель управления (Start/Stop/Save/Copy).

### 1.3. Публичный API (черновой контракт)

```csharp
public sealed class RecorderOptions
{
    public string? OutputDirectory { get; init; }
    public string ScenarioName { get; init; } = "Scenario";
    public RecorderHotkeys Hotkeys { get; init; } = RecorderHotkeys.Default;
    public SelectorOptions Selector { get; init; } = new();
    public CodegenOptions Codegen { get; init; } = new();
    public IList<IAssertValueExtractor> AssertExtractors { get; } = new List<IAssertValueExtractor>();
}

public static class TestRecorder
{
    // Подключает рекордер к окну/TopLevel
    public static IRecorderSession Attach(Window window, RecorderOptions? options = null);
}

public interface IRecorderSession : IDisposable
{
    RecorderState State { get; }
    void Start();
    void Stop();
    void Pause();
    void Resume();

    // Сохранить в файл и вернуть путь
    string SaveTestToFile();

    // Получить текст кода (для copy/paste)
    string ExportTestCode();
}
```

### 1.4. Ограничения и нефункциональные требования

* По умолчанию рекордер **не должен влиять на production**:

  * рекомендовано включать только в `DEBUG` (условная компиляция) либо требовать явного флага запуска
* Потокобезопасность: события UI только на UI thread, запись в коллекции без блокировок (UI thread only)
* Производительность: не писать “каждый пиксель мыши”; hover/движения — только по команде/триггеру
* Безопасность: возможность маскировать ввод (например, `PasswordBox`) — не записывать реальный текст, а вставлять `"<redacted>"` или `ui.TypeSecret(...)`

---

## 2) Компонент №2: NuGet библиотека тест‑хелперов (для test проекта)

### 2.1. Название и пакет

* Рабочее имя: `Avalonia.HeadlessTestKit`
* Тип: .NET class library (NuGet)
* Цель: обеспечить DSL/обёртки + устойчивые ожидания/поиск элементов

### 2.2. Основные функции

**H1. DSL класс `Ui`**
Методы (минимум):

* `Click(id)`
* `RightClick(id)` (опц.)
* `DoubleClick(id)` (опц.)
* `Hover(id)`
* `Scroll(id, Vector delta)`
* `TypeText(id, text)` (через `KeyTextInput`)
* `KeyPress(PhysicalKey, modifiers)` / `KeyChord(...)`
* `AssertText(id, expected)`
* `AssertTrue(id, predicate)` (опц.)
* `WaitFor(id, condition, timeout)` — обязательный, чтобы тесты были стабильны

**H2. Поиск элемента**

* `FindControl(id)`:

  * AutomationId → Name → (опц.) fallback с диагностикой
* Выбор точки взаимодействия:

  * центр `Bounds`, трансляция координат в окно через `TranslatePoint`
  * (опц.) режим “клика по конкретной точке элемента” для сложных случаев

**H3. Синхронизация**

* После каждого шага:

  * прогон UI jobs/диспетчера (helper внутри DSL)
  * (опц.) короткий `WaitForIdle()` / обработка layout/render pipeline
* Для async сценариев:

  * `WaitForText(...)`, `WaitForVisible(...)`, `WaitForEnabled(...)`

**H4. Интеграция test framework**

* В составе примера — xUnit **или** NUnit:

  * builder `AppBuilder.Configure<App>().UseHeadless(...)`
  * атрибут/фикстура инициализации headless платформы

---

## 3) Пример приложения Avalonia (подключен рекордер)

### 3.1. Репозиторий/проект

* `samples/SampleApp` (Avalonia Desktop, MVVM)
* Включает сценарии для демонстрации:

  * форма логина/поиска (TextBox + Button + status TextBlock)
  * список со скроллом (ListBox/ScrollViewer)
  * контрол с tooltip/hover‑состоянием (для assert capture)

### 3.2. Требования к приложению

* На ключевых контролах проставлены `AutomationProperties.AutomationId`
* Подключение рекордера:

  * в `App.OnFrameworkInitializationCompleted` или в `MainWindow`:

    * если включен флаг записи → `TestRecorder.Attach(mainWindow, options)`
* Пример конфигурации:

  * OutputDirectory = `./RecordedTests`
  * ScenarioName берётся из UI (например, поле ввода в настройках рекордера) или дефолт

### 3.3. UX демонстрации

* Оверлей показывает:

  * “Recording” + счётчик шагов
  * куда сохранится файл
* Документация в README: “как накликать → сохранить → вставить в тесты → запустить”

---

## 4) Пример тестового проекта (headless тесты приложения + хелперы)

### 4.1. Репозиторий/проект

* `samples/SampleApp.Tests.Headless`
* Подключены:

  * `Avalonia.Headless*` пакеты
  * `Avalonia.HeadlessTestKit` (из п.2)
  * проект/сборка `SampleApp` (или ссылка на сборку UI)

### 4.2. Содержимое

* `TestAppBuilder` / инициализация headless платформы
* `Ui` DSL/обертка (из NuGet)
* 2–3 примера тестов:

  1. **Сгенерированный тест** (файл, созданный рекордером)
  2. Тест со “вручную допиленным” шагом (демонстрация как расширять)
  3. Пример `WaitFor(...)` для асинхронной реакции UI

### 4.3. Запуск

* Команда: `dotnet test`
* CI-friendly: не требует реального экрана/оконного менеджера

---

# Формат артефактов и структура репозитория (рекомендация)

```
/src
  /Avalonia.TestRecorder
  /Avalonia.HeadlessTestKit
/samples
  /SampleApp
  /SampleApp.Tests.Headless
/docs
  recorder-usage.md
  conventions-automationid.md
```

---

# Acceptance Criteria (критерии приёмки)

1. Подключаем `Avalonia.TestRecorder` к SampleApp, включаем запись флагом/горячей клавишей.
2. Прокликиваем сценарий: клик → ввод текста → скролл → клик → capture assert.
3. Нажимаем Save — появляется `.cs` файл с тестом в указанной папке.
4. В SampleApp.Tests.Headless тест **компилируется и проходит** в headless режиме.
5. В отчёте рекордера есть предупреждения, если использованы fallback‑селекторы (не AutomationId).