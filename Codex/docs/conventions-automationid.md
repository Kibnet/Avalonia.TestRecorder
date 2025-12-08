# Конвенции AutomationId

Почему: рекордер и DSL опираются на `AutomationProperties.AutomationId` для стабильных селекторов. Если ID нет, в тест попадает путь по дереву/координаты с предупреждением.

## Базовые правила
- Уникальность в пределах окна/диалога.
- PascalCase без пробелов (`LoginButton`, `ItemsList`, `HoverTarget`).
- Для текстовых полей — суффикс `Input`/`Box` (`SearchBox`, `EmailInput`).
- Для статусов/лейблов — суффикс `Text` (`StatusText`, `ErrorText`).
- Для списков/таблиц — `List`, `Grid`, `Tree` + сущность (`OrdersList`, `UsersGrid`).
- Для действий — глагол (`SaveButton`, `RefreshButton`, `RetryLink`).

## Списки и шаблоны
- Контейнер списка: `ItemsList`.
- Внутри `DataTemplate` добавляйте часть, которую будет видно из VM (например, `ItemTitle`, `DeleteButton`), если нужно взаимодействовать с конкретным элементом.
- Если генерируете ID на лету, используйте стабильный ключ модели (`Order-{OrderId}`).

## Диалоги и всплывающие окна
- Префикс по контексту: `LoginDialog.SubmitButton`, `ConfirmDialog.CancelButton`.
- Для уведомлений/overlay — `Toast.Message`, `Toast.CloseButton`.

## Проверка перед записью
- Пройдитесь по критичным сценариям и убедитесь, что каждый кликаемый/валидируемый контрол имеет `AutomationId`.
- Для спорных мест (динамические списки, кастомные контролы) добавьте fallback ID в VM или код-бихайнд.
