# Avalonia.TestRecorder — быстрое получение headless-тестов

## Как включить запись
- Запустите `SampleApp` с флагом `AV_RECORDER=1` **или** аргументом `--record-tests`. В Debug он также подключается автоматически.
- В окне появится маленький оверлей с состоянием и путём вывода (по умолчанию `./RecordedTests` рядом с exe).
- Горячие клавиши:
  - `Ctrl+Shift+R` — старт/стоп записи
  - `Ctrl+Shift+P` — пауза/возобновление
  - `Ctrl+Shift+S` — сохранить тест в файл
  - `Ctrl+Shift+A` — захват assert по контролу под курсором или фокусу

## Что записывается
- Клики (Left/Right) с приоритетом селектора `AutomationId`, затем `Name`, затем путь по дереву, в крайнем случае координаты (в коде будет `// WARNING: fallback selector`).
- Ввод текста объединяется в один шаг `TypeText`.
- Спец-клавиши (`Enter`, `Tab`, `Backspace`, `Delete`, `Escape`, стрелки) пишутся отдельными шагами.
- Скролл добавляет шаг `Scroll`.
- Assert-capture читает `TextBox.Text`, `TextBlock.Text`, `ContentControl.Content`, `ToggleButton.IsChecked`, `IsVisible`, `IsEnabled`. Можно расширить через `IAssertValueExtractor`.

## Сохранение и генерация кода
- Файл: `{AppName}.{ScenarioName}.{yyyyMMdd_HHmmss}.g.cs` в папке вывода (`RecorderOptions.OutputDirectory`).
- Код использует `Avalonia.HeadlessTestKit`: `HeadlessTestApplication.StartAsync<App, MainWindow>()` и DSL `Ui`.
- Namespace и префикс класса задаются через `RecorderOptions.Codegen`.

## Быстрый сценарий (SampleApp)
1) `Ctrl+Shift+R` — начать запись.  
2) Клик по `SearchBox`, введите текст, проскрольте `ItemsList`, нажмите `Найти`.  
3) `Ctrl+Shift+A` на статусе, чтобы записать assert.  
4) `Ctrl+Shift+S` — сохраните тест, файл появится в `RecordedTests`.  
5) Перенесите файл в `samples/SampleApp.Tests.Headless` и выполните `dotnet test`.

## Использование в своём приложении
- Подключите NuGet `Avalonia.TestRecorder` и вызывайте `TestRecorder.Attach(mainWindow, options)` (желательно только в DEBUG/по флагу).
- Поставьте `AutomationProperties.AutomationId` на интерактивные контролы — это основной стабильный селектор.
- Для кастомных контролов добавьте свой `IAssertValueExtractor`.
