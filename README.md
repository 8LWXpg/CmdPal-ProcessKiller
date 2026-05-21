# Process Killer for CmdPal

<a href="https://apps.microsoft.com/detail/9PNHK9LDHMHS?referrer=appbadge&mode=full" target="_blank"  rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

## Usage

### Kill a Process

![kill a process](./assets/process.png)

### Kill a Process by Port

![kill a process by port](./assets/port.png)

## Manual Installation

**Prerequisite**

- [winapp cli](https://github.com/microsoft/winappCli): `winget install Microsoft.WinAppCli`

**Steps**

1. Download both `msixbundle` and `cert.pfx`
2. `sudo winapp cert install cert.pfx` (or open terminal with admin privilege if you don't have sudo, only need to do this once)
3. Click to install or `Add-AppxPackage <msix>`

