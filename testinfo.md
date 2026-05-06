# Testing Instructions

## Prerequisites

### Required Software
1. **PowerToys with Command Palette**
   - Install from Microsoft Store: https://apps.microsoft.com/detail/xp89dcgq3k6vld
   - Includes Microsoft Edge WebView2 Runtime
   - Available for both Windows 11 and Windows 10

## Installation Steps

### Step 1: Install PowerToys
1. Open Microsoft Store
2. Search for "PowerToys" or use the direct link above
3. Click "Get" or "Install"
4. Wait for installation to complete

### Step 2: Install Extension
1. Download the MSIX package
2. Right-click the MSIX file
3. Select "Install" from the context menu
4. Follow the installation prompts
5. The extension will automatically register with Command Palette

### Step 3: Activate Command Palette
1. Open PowerToys
2. Navigate to Command Palette tool
3. Enable Command Palette if not already active
4. Set your preferred hotkey (default: Win+ Alt + Space)

## Testing the Extension

### Basic Functionality Test
1. Press your Command Palette hotkey
2. In the search box, type "Reload"
3. Select "Reload, Reload Command Palette extensions" and press Enter
4. Start a program (for example, Microsoft Edge)
5. Type "Kill a Process" in the search box
6. The list of processes should appear as in preview screenshot
7. Search for "msedge"
8. Press Ctrl+Enter to kill all "msedge.exe"

### Expected Behavior

msedge.exe should be killed.

## Troubleshooting

### Extension Not Appearing
- Restart PowerToys
- Reload Command Palette extensions (Step 3 in testing)
- Verify the MSIX package installed successfully in Windows Settings > Apps


