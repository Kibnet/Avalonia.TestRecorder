# AutomationId Conventions

## Why AutomationId Matters

`AutomationId` is the **most stable** way to identify UI elements in tests. Unlike Name properties or visual tree paths, AutomationIds:

- ✅ Survive UI refactoring and layout changes
- ✅ Work across different themes and styles  
- ✅ Are specifically designed for automation and accessibility
- ✅ Don't conflict with other identifiers

## Setting AutomationId

### In XAML

```xml
<Button AutomationProperties.AutomationId="submitButton" Content="Submit" />
<TextBox AutomationProperties.AutomationId="emailField" />
<TextBlock AutomationProperties.AutomationId="statusLabel" />
```

### In Code

```csharp
var button = new Button { Content = "Submit" };
AutomationProperties.SetAutomationId(button, "submitButton");
```

## Naming Conventions

### Recommended Patterns

**Pattern 1: Semantic Name + Control Type**
```xml
<Button AutomationProperties.AutomationId="loginButton" />
<TextBox AutomationProperties.AutomationId="usernameField" />
<ListBox AutomationProperties.AutomationId="itemList" />
```

**Pattern 2: Hierarchical Naming**
```xml
<StackPanel AutomationProperties.AutomationId="loginPanel">
    <TextBox AutomationProperties.AutomationId="loginPanel_username" />
    <TextBox AutomationProperties.AutomationId="loginPanel_password" />
    <Button AutomationProperties.AutomationId="loginPanel_submit" />
</StackPanel>
```

### Naming Guidelines

✅ **DO:**
- Use descriptive, semantic names: `saveButton`, `searchField`, `resultList`
- Use camelCase or snake_case consistently
- Include context for clarity: `userProfile_editButton`
- Keep names unique within your window
- Update AutomationIds when functionality changes

❌ **DON'T:**
- Use generic names: `button1`, `textBox2`, `panel3`
- Use implementation details: `stackPanel_0`, `grid_child_1`
- Change AutomationIds without updating tests
- Reuse IDs for different controls
- Use special characters or spaces

## Adding AutomationId to Existing Applications

### 1. Audit Your UI

Find controls without AutomationId:

```csharp
// Extension method to find controls missing AutomationId
public static void AuditAutomationIds(Visual root)
{
    foreach (var control in root.GetVisualDescendants().OfType<Control>())
    {
        var id = AutomationProperties.GetAutomationId(control);
        if (string.IsNullOrEmpty(id) && control is Button or TextBox or TextBlock)
        {
            Console.WriteLine($"Missing AutomationId: {control.GetType().Name}");
        }
    }
}
```

### 2. Prioritize Critical Paths

Focus first on:
1. Login/authentication flows
2. Primary user actions (save, submit, delete)
3. Navigation elements
4. Input forms
5. Status indicators

### 3. Document Your IDs

Maintain a registry of AutomationIds in your project:

```markdown
## Login View
- `usernameField` - Username input
- `passwordField` - Password input
- `loginButton` - Submit login
- `forgotPasswordLink` - Password recovery link
- `statusLabel` - Login status message
```

## Common Patterns

### Forms

```xml
<StackPanel>
    <TextBox AutomationProperties.AutomationId="firstName" />
    <TextBox AutomationProperties.AutomationId="lastName" />
    <TextBox AutomationProperties.AutomationId="email" />
    <Button AutomationProperties.AutomationId="saveContact" />
</StackPanel>
```

### Lists and Grids

```xml
<ListBox AutomationProperties.AutomationId="contactList">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock AutomationProperties.AutomationId="{Binding Id, StringFormat='contact_{0}_name'}" />
                <Button AutomationProperties.AutomationId="{Binding Id, StringFormat='contact_{0}_edit'}" />
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Dialogs

```xml
<Window AutomationProperties.AutomationId="confirmDialog">
    <StackPanel>
        <TextBlock AutomationProperties.AutomationId="confirmDialog_message" />
        <Button AutomationProperties.AutomationId="confirmDialog_ok" />
        <Button AutomationProperties.AutomationId="confirmDialog_cancel" />
    </StackPanel>
</Window>
```

## Migration Checklist

- [ ] Identify all interactive controls in your application
- [ ] Add AutomationId to buttons, textboxes, and key navigation elements
- [ ] Document your ID naming convention
- [ ] Update existing tests to use AutomationId selectors
- [ ] Add AutomationId requirement to code review checklist
- [ ] Train team on naming conventions
- [ ] Set up automated auditing in CI/CD

## Benefits Beyond Testing

AutomationIds also improve:
- **Accessibility**: Screen readers use AutomationId for better navigation
- **UI Automation**: External automation tools can interact with your app
- **Debugging**: Easier to identify controls in logs and diagnostics
- **Monitoring**: Track user interactions by AutomationId for analytics

## Resources

- [Avalonia AutomationProperties Documentation](https://docs.avaloniaui.net/)
- [Accessibility Best Practices](https://docs.avaloniaui.net/)
