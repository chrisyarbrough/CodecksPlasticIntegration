# Codecks Integration for PlasticSCM / Unity DevOps #

![](Images/ExtensionPreview.png "Extension Preview")

Adds support for the [Codecks](https://www.codecks.io/) issue tracker to [PlasticSCM](https://www.plasticscm.com/).

> PlasticSCM was acquired by Unity in 2020 and is now part of Unity DevOps.
> The underlying API (and this extension) should still work the same though.

# Getting Started - User Setup

1) Assuming you already use PlasticSCM or Unity DevOps.
   The client can be downloaded [here](https://www.plasticscm.com/download).

2) [Download](https://github.com/chrisyarbrough/CodecksPlasticIntegration/releases)
   or build the CodecksExtension library (zip file with DLLs).

3) Place the `codecks` folder (containing the plugin and its dependencies)
   in the extensions directory of the PlasticSCM installation.
   With the default installation, the path should look like this:
	- Windows: `C:\Program Files\PlasticSCM5\client\extensions\codecks\CodecksExtension.dll`
	- macOS: `/Applications/PlasticSCM.app/Contents/extensions/codecks/CodecksExtension.dll`

   ![](Images/PlasticSCM_Configuration.png "PlasticSCM Preferences Window")

4) Find the file `customextensions.conf`:
	- Windows: `C:\Program Files\PlasticSCM5\client\customextensions.conf`
	- macOS: `/Applications/PlasticSCM.app/Contents/MacOS/customextensions.conf`

   Add the following line to the file:
   > Codecks=extensions/codecks/CodecksExtension.dll

   You will need admin permissions to edit this file.

5) Open the preferences in the PlasticSCM GUI and configure the Codecks extension with your personal settings.

   ![](Images/PlasticSCM_Preferences.png "PlasticSCM Preferences Window")

   Note that 'Account Name' is the subdomain of your organization used for the Codecks web frontend.

---

# Developer Setup

## Building from Source

**Prerequisites**

- .NET 7.0 SDK
- PlasticSCM 10.0.16.6505 (or newer)

Now testing: 11.0.16.9080

Of course the plugin should work in other (if not all) versions of PlasticSCM, but this specific one is known to work.
The beta GUI (PlasticX) of the same version is also supported internally.
At the time of writing, PlasticX allows creating a new branch from a task, but doesn't show task info anywhere else.

The codecks extension depends on libraries provided by PlasticSCM (e.g. issuetrackerinterface.dll).
In theory, these dependencies should be provided by the host process (the Plastic GUI), however,
the new beta GUI PlasticX does not load the utils.dll. For this reason, it was decided to
simply copy-paste the libraries during development and deploy them right next to the extension.
See the 'Libraries' folder in the repository.

The project solution includes a "Start Host" configuration which builds the extension and copies it directly to
the default PlasticSCM installation path and also launch the GUI client for interactive testing and debugging.
Most likely, your IDE will need to be started with admin permissions for this to work.

## Logging

In the `~/.plastic4/plasticx.log.conf` add:

```xml
<logger name="Codecks">
  <level value="DEBUG" />
  <appender-ref ref="DebugAppender" />
</logger>
```

On windows, the file is likely in the installation directory.

## Testing

For the regular tests:

```bash
dotnet test
```

To run the _CodecksServiceTest_ configure your dotnet user secrets like so:

```bash
dotnet user-secrets set key value
```

with the following info:

| Key              | Description                                                         |
|------------------|---------------------------------------------------------------------|
| Codecks:Email    | Email address of your Codecks account.                              |
| Codecks:Password | Password associated with the Codecks email.                         |
| Codecks:Account  | Account name of your Codecks organization (not your personal name). |

## Additional Resources

The process for developing and configuring PlasticSCM extensions is documented
[here](https://www.plasticscm.com/documentation/extensions/plastic-scm-version-control-task-and-issue-tracking-guide#WritingPlasticSCMcustomextensions).

Codecks provides the [web API](https://manual.codecks.io/api/)
which is used by the extension to fetch task information and post status updates.
As of August 2023, the API is still in beta, but it has been very stable since 2022.

## Contributing

If you'd like to contribute or have any trouble using the extension, open a new issue to discuss it.
If you already have a working fix in place, please open a Pull Request, thank you!
