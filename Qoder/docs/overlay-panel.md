# Recorder Overlay Panel

## Overview

The Recorder Overlay Panel is a compact UI control that provides visual feedback and control over the test recording session. It appears automatically at the top of the window when the TestRecorder is attached.

## Features

The overlay panel includes the following elements:

### Status Indicator
- **Gray circle** ⚪ - Recorder is OFF
- **Red circle** 🔴 - Recording in progress
- **Yellow square** 🟨 - Recording is PAUSED

### Step Counter
Displays the number of steps currently recorded in the session (e.g., "5 steps").

### Control Buttons

#### 1. Record/Stop Button (▶️/⏹️)
- **Icon changes** based on state:
  - Play icon (▶️) when not recording
  - Stop icon (⏹️) when recording
- **Action**: Toggles between recording and stopped states
- **Hotkey**: Ctrl+Shift+R

#### 2. Pause/Resume Button (⏸️)
- **Icon**: Pause symbol
- **Action**: Pauses active recording or resumes from pause
- **Enabled**: Only when recording or paused
- **Hotkey**: Ctrl+Shift+P

#### 3. Clear Button (🗑️)
- **Icon**: Trash bin
- **Action**: Clears all recorded steps without stopping the session
- **Enabled**: Only when steps are recorded
- **Note**: Does not prompt for confirmation

#### 4. Save Button (💾)
- **Icon**: Floppy disk/save symbol
- **Action**: Opens a file picker dialog to save the test code
- **File type**: C# source files (*.cs)
- **Default name**: Auto-generated based on app name, scenario, and timestamp
- **Enabled**: Only when steps are recorded
- **Hotkey**: Ctrl+Shift+S

#### 5. Minimize Button (➖)
- **Icon**: Minus symbol
- **Action**: Minimizes the overlay to a small button that can be restored
- **Note**: The recorder session continues running; only the UI is minimized

#### 6. Restore Button (➕)
- **Icon**: Plus symbol
- **Action**: Restores the overlay to its full size
- **Note**: Appears only when the overlay is minimized

## Configuration

### Showing/Hiding the Overlay

By default, the overlay is shown when the recorder is attached. You can control this behavior:

```csharp
TestRecorder.Attach(mainWindow, new RecorderOptions
{
    ShowOverlay = true  // Default: true
});
```

### Theme Support

The overlay automatically detects the application theme (light or dark) and adapts its appearance:

**Auto-detection** (default):
The overlay checks the following in order:
1. `ThemeVariant` resource (looks for "Dark" in the variant name)
2. Background color brightness (calculates luminance)
3. Falls back to light theme if unable to detect

**Manual theme setting**:
```csharp
// Set at initialization
TestRecorder.Attach(mainWindow, new RecorderOptions
{
    OverlayTheme = OverlayTheme.Dark  // or OverlayTheme.Light
});

// Change theme later
var overlay = // ... get reference to overlay
overlay.SetTheme(isDark: true);
```

**Theme Colors:**

Light Theme:
- Background: #F0F0F0 (Light gray)
- Border: #C0C0C0 (Medium gray)
- Text: #000000 (Black)
- Icons: #95A5A6 (Gray)

Dark Theme:
- Background: #2D2D30 (Dark gray)
- Border: #3F3F46 (Darker gray)
- Text: #FFFFFF (White)
- Icons: #B0B0B0 (Light gray)

### Overlay Placement

The overlay is displayed as a **separate modal window** to avoid disrupting the structure of the window being recorded:

- **Position**: Centered at the top edge of the main window
- **Window type**: Separate, frameless, topmost window
- **Behavior**: Follows the main window when moved, hides when minimized
- **Z-order**: Always on top (Topmost = true)
- **Size**: 
  - Expanded: 200x40px
  - Minimized: 200x30px
- **Styling**: Rounded bottom corners (6px radius) with drop shadow for floating appearance
- **Transparency**: Transparent background with non-transparent content panel

**Visual characteristics:**
- Drop shadow: 12px blur radius, 4px Y-offset, 30% opacity
- Border: 1px all around (theme-dependent color)
- Background: Semi-opaque panel (theme-dependent color)
- Window decorations: None (SystemDecorations.None)
- Not shown in taskbar

**Window tracking:**
- Automatically repositions when main window moves
- Hides when main window is minimized
- Closes when main window closes
- Does not interfere with main window's visual tree or layout

## Visual Design

**Dimensions:**
- Expanded Height: 40px
- Minimized Height: 30px
- Background: Light gray (#F0F0F0)
- Border: Bottom border only (#C0C0C0)
- Button size: 32x32px (expanded), 30x30px (minimized)
- Icon size: 20x20px (expanded), 18x18px (minimized)

**Colors:**
- Record/Play: Green (#27AE60)
- Stop: Red (#E74C3C)
- Pause: Orange (#F39C12)
- Save: Blue (#3498DB)
- Clear/Minimize/Restore: Gray (#95A5A6)

## Workflow Example

1. **Launch app** - Overlay appears at top of window
2. **Click ▶️** - Status changes to red circle, recording starts
3. **Interact with UI** - Step counter increments
4. **Click ⏸️** - Status changes to yellow square, recording pauses
5. **Click ⏸️ again** - Status returns to red circle, recording resumes
6. **Click 💾** - File picker appears
7. **Choose location** - Test file is saved
8. **Click 🗑️** - Step counter resets to 0
9. **Click ➖** - Overlay minimizes to a small button
10. **Click ➕** - Overlay restores to full size

## Keyboard-Only Workflow

For users who prefer keyboard shortcuts, the overlay can be completely ignored:

1. Press **Ctrl+Shift+R** to start recording
2. Perform test actions
3. Press **Ctrl+Shift+A** to capture assertions
4. Press **Ctrl+Shift+S** to save (file picker still appears)

## Implementation Details

### Update Frequency

The overlay UI updates every 100ms using a `DispatcherTimer` to reflect:
- Current recorder state
- Step count
- Button enabled states

### Event Handling

All button clicks invoke methods on the attached `RecorderSession`:
- Record button → `Start()` or `Stop()`
- Pause button → `Pause()` or `Resume()`
- Clear button → `ClearSteps()`
- Save button → `SaveTestToFile(filePath)` after file picker
- Minimize button → `Minimize()` (does not affect session)
- Restore button → `Restore()` (does not affect session)

### File Picker Integration

The Save button uses Avalonia's native file picker:

```csharp
var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
{
    Title = "Save Test Code",
    SuggestedFileName = session.GetSuggestedFileName(),
    FileTypeChoices = new[] { new FilePickerFileType("C# Source File") { Patterns = new[] { "*.cs" } } },
    DefaultExtension = "cs"
});
```

## Troubleshooting

### Overlay Not Appearing
- Check that `RecorderOptions.ShowOverlay` is `true` (default)
- Verify window has content set before attaching recorder
- Check logs for any initialization errors

### Overlay Blocks UI Elements
- The overlay is a separate window, so it won't block any UI elements in the main window
- If the overlay obscures important content, you can move the main window
- Click the ➖ button to minimize the overlay temporarily
- Consider setting `ShowOverlay = false` and using hotkeys only

### Save Button Does Nothing
- Ensure at least one step is recorded (step counter > 0)
- Check that the file picker has permission to access the chosen directory
- Review logs for file I/O errors

## Customization (Future)

Potential enhancements not yet implemented:
- Draggable overlay position
- ~~Collapsible/expandable panel~~ (Implemented: Minimize/Restore functionality)
- Custom theme colors
- Configurable button visibility
- Mini/full mode toggle