Imports ShowMeTheXAML

Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        XamlDisplay.Init()
        MyBase.OnStartup(e)
    End Sub
End Class
