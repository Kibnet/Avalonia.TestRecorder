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

#### 5. Close Button (✕)
- **Icon**: X symbol
- **Action**: Hides the overlay panel
- **Note**: The recorder session continues running; only the UI is hidden

## Configuration

### Showing/Hiding the Overlay

By default, the overlay is shown when the recorder is attached. You can control this behavior:

```csharp
TestRecorder.Attach(mainWindow, new RecorderOptions
{
    ShowOverlay = true  // Default: true
});
```

### Overlay Placement

The overlay is automatically positioned at the top of the window and spans the full width. It adjusts based on the window content:

- If window content is a `Panel`, the overlay is inserted at the beginning
- If window content is a single `Control`, it's wrapped in a `DockPanel` with the overlay docked to the top

## Visual Design

**Dimensions:**
- Height: 40px
- Background: Light gray (#F0F0F0)
- Border: Bottom border only (#C0C0C0)
- Button size: 32x32px
- Icon size: 20x20px

**Colors:**
- Record/Play: Green (#27AE60)
- Stop: Red (#E74C3C)
- Pause: Orange (#F39C12)
- Save: Blue (#3498DB)
- Clear/Close: Gray (#95A5A6)

## Workflow Example

1. **Launch app** - Overlay appears at top of window
2. **Click ▶️** - Status changes to red circle, recording starts
3. **Interact with UI** - Step counter increments
4. **Click ⏸️** - Status changes to yellow square, recording pauses
5. **Click ⏸️ again** - Status returns to red circle, recording resumes
6. **Click 💾** - File picker appears
7. **Choose location** - Test file is saved
8. **Click 🗑️** - Step counter resets to 0
9. **Click ✕** - Overlay hides (session still active)

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
- Close button → Hides overlay (does not affect session)

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
- The overlay is only 40px tall and docked at the top
- Click the ✕ button to hide it temporarily
- Consider setting `ShowOverlay = false` and using hotkeys only

### Save Button Does Nothing
- Ensure at least one step is recorded (step counter > 0)
- Check that the file picker has permission to access the chosen directory
- Review logs for file I/O errors

## Customization (Future)

Potential enhancements not yet implemented:
- Draggable overlay position
- Collapsible/expandable panel
- Custom theme colors
- Configurable button visibility
- Mini/full mode toggle
