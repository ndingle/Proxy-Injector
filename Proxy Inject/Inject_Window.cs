namespace Proxy_Inject
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("a805b931-88c8-462e-9d4f-c471c51f0b83")]
    public class Inject_Window : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Inject_Window"/> class.
        /// </summary>
        public Inject_Window() : base(null)
        {
            this.Caption = "Proxy Injector";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new Inject_WindowControl();
        }
    }
}
