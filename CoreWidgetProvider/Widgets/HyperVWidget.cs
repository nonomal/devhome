// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Antlr4.Runtime.Misc;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;
using Windows.Foundation;
using Windows.Media.Ocr;

namespace CoreWidgetProvider.Widgets;

public enum ComputeSystemProviderOperation
{
    CreateComputeSystem = 0x00000001,
}

public enum ComputeSystemType
{
    HyperV = 0,
    DevBox = 1,
    WSL = 2,
    WSA = 3,
    Container = 4,
    Docker = 5,
    Codespace = 6,
    AzureVM = 7,
    VMware = 8,
    VMwareCloud = 9,
    VirtualBox = 10,
    GoogleCloud = 11,
    Custom = 12,
}

public enum ComputeSystemOperation
{
    Start = 0x00000001,
    ShutDown = 0x00000002,
    Terminate = 0x00000004,
    Delete = 0x00000008,
    Save = 0x00000010,
    Pause = 0x00000020,
    Resume = 0x00000040,
    CreateSnapshot = 0x00000100,
    ApplySnapshot = 0x00000200,
    DeleteSnapshot = 0x00000400,
    ModifyProperties = 0x00000800,
    ApplyConfiguration = 0x00001000,
}

public enum ComputeSystemState
{
    Created = 0x00000001,
    Running = 0x00000002,
    Paused = 0x00000004,
    Stopped = 0x00000008,
    Saved = 0x00000010,
    SavedAsTemplate = 0x00000020,
    ShuttingDown = 0x00000040,
    Starting = 0x00000080,
    Unknown = 0x00000100,
}

public interface IComputeSystem
{
    string Name { get; }

    string Id { get; }

    ComputeSystemType Type { get; }

    ComputeSystemOperation SupportedOperations { get; }

    Windows.Foundation.IAsyncOperation<ComputeSystemState> GetState(string? options);

    Windows.Foundation.IAsyncOperation<string> Start(string options);

    Windows.Foundation.IAsyncOperation<string> ShutDown(string options);

    Windows.Foundation.IAsyncOperation<string> Terminate(string options);

    Windows.Foundation.IAsyncOperation<string> Delete(string options);

    Windows.Foundation.IAsyncOperation<string> Save(string options);

    Windows.Foundation.IAsyncOperation<string> Pause(string options);

#pragma warning disable CA1716 // Identifiers should not match keywords
    Windows.Foundation.IAsyncOperation<string> Resume(string options);
#pragma warning restore CA1716 // Identifiers should not match keywords

    Windows.Foundation.IAsyncOperation<string> CreateSnapshot(string options);

    Windows.Foundation.IAsyncOperation<string> ApplySnapshot(string options);

    Windows.Foundation.IAsyncOperation<string> DeleteSnapshot(string options);

    Windows.Foundation.IAsyncOperation<string> GetProperties(string options);

    Windows.Foundation.IAsyncOperation<string> ModifyProperties(string properties);

    Windows.Foundation.IAsyncOperation<string> Connect(string properties);

    Windows.Foundation.IAsyncOperation<string> ApplyConfiguration(string configuration);
}

public interface IComputeSystemProvider
{
    string DisplayName { get; }

    string Id { get; }

    string Properties { get; }

    ComputeSystemType Type { get; }

    ComputeSystemProviderOperation SupportedOperations { get; }

    string DefaultComputeSystemProperties
    {
        get; set;
    }

    Windows.Foundation.IAsyncOperation<IEnumerable<IComputeSystem>> GetComputeSystemsAsync(IDeveloperId? developerId, string? options);

    Windows.Foundation.IAsyncOperation<IComputeSystem> CreateComputeSystemAsync(string options);
}

internal class HyperVProvider : IComputeSystemProvider
{
#pragma warning disable SA1310 // Field names should not contain underscore
    private const int SHORT_ID_LENGTH = 6;
#pragma warning restore SA1310 // Field names should not contain underscore

    // This is not a unique identifier, but is easier to read in a log and highly unlikely to
    // match another running widget.
    protected string ShortId => Id.Length > SHORT_ID_LENGTH ? Id[..SHORT_ID_LENGTH] : Id;

    protected static readonly string Name = nameof(HyperVProvider);

    public string DisplayName => "Hyper-V";

    public string Id => "Microsoft.Provider.HyperV";

    public string Properties => string.Empty;

    public ComputeSystemType Type => ComputeSystemType.HyperV;

    public ComputeSystemProviderOperation SupportedOperations => ComputeSystemProviderOperation.CreateComputeSystem;

    public string DefaultComputeSystemProperties
    {
        get; set; // TODO: Force valid JSON string.
    }

    public Windows.Foundation.IAsyncOperation<IEnumerable<IComputeSystem>> GetComputeSystemsAsync(IDeveloperId? developerId, string? options)
    {
        try
        {
            return Task.Run(() => EnumerateVMs()).AsAsyncOperation();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, $"Could not enumerate Hyper-V VMs (async).", ex);
            throw;
        }
    }

    public Windows.Foundation.IAsyncOperation<IComputeSystem> CreateComputeSystemAsync(string options)
    {
        throw new NotImplementedException();
    }

    public HyperVProvider()
    {
        DefaultComputeSystemProperties = string.Empty;
    }

    private static ManagementObjectCollection GetWmiObjects(string managementScope, string className, string where = "")
    {
        var scope = string.IsNullOrWhiteSpace(managementScope)
            ? new ManagementScope()
            : new ManagementScope(managementScope);

        var query = string.IsNullOrWhiteSpace(where)
            ? $"select * from {className}"
            : $"select * from {className} where {where}";

        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
        return searcher.Get();
    }

    private IEnumerable<IComputeSystem> EnumerateVMs()
    {
        const string ManagementScopeHyperV = @"root\virtualization\v2";
        Log.Logger()?.ReportDebug(Name, ShortId, $"Start enumerating Hyper-V VMs.");

        try
        {
            ManagementObjectCollection vms = GetWmiObjects(ManagementScopeHyperV, "Msvm_ComputerSystem");
            List<HyperVVM> hyperVVMs = new List<HyperVVM>();
            foreach (var vm in vms)
            {
                try
                {
                    ManagementObject current = (ManagementObject)vm;
                    var vmName = current.GetPropertyValue("ElementName");
                    Log.Logger()?.ReportDebug(Name, ShortId, $"Found {vmName} Hyper-V VM.");
                    hyperVVMs.Add(new HyperVVM(current));
                }
                catch (Exception ex)
                {
                    Log.Logger()?.ReportError(Name, ShortId, "Error reading one of Hyper-V VMs.", ex);
                }
            }

            Log.Logger()?.ReportDebug(Name, ShortId, $"Finished enumerating Hyper-V VMs.");
            return hyperVVMs;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, $"Could not enumerate Hyper-V VMs.", ex);
            throw;
        }
    }
}

internal enum HyperVState
{
    Unknown = 0,
    Other = 1,
    Enabled = 2,
    Disabled = 3,
    ShuttingDown = 4,
    NotApplicable = 5,
    EnabledOffline = 6,
    InTest = 7,
    Deferred = 8,
    Quiesce = 9,
    Starting = 10,
}

internal class HyperVVM : IComputeSystem
{
    public HyperVVM(ManagementObject wmiVM)
    {
        this.wmiVM = wmiVM;
    }

    private readonly ManagementObject wmiVM;

    public string Name => (string)wmiVM.GetPropertyValue("ElementName");

    public string Id => "Microsoft.Hyper-V";

    public ComputeSystemType Type => ComputeSystemType.HyperV;

    public ComputeSystemOperation SupportedOperations =>
        ComputeSystemOperation.Start |
        ComputeSystemOperation.ShutDown |
        ComputeSystemOperation.Pause |
        ComputeSystemOperation.Resume;

    private ComputeSystemState GetVmState()
    {
        try
        {
            ComputeSystemState state = ComputeSystemState.Unknown;
            var hyperVState = (ushort)wmiVM.GetPropertyValue("EnabledState");

            switch ((HyperVState)hyperVState)
            {
                case HyperVState.Unknown:
                case HyperVState.Other:
                    state = ComputeSystemState.Unknown;
                    break;
                case HyperVState.Enabled:
                    state = ComputeSystemState.Running;
                    break;
                case HyperVState.Disabled:
                    state = ComputeSystemState.Stopped;
                    break;
                case HyperVState.ShuttingDown:
                    state = ComputeSystemState.ShuttingDown;
                    break;

                case HyperVState.EnabledOffline:
                    state = ComputeSystemState.Saved;
                    break;
                case HyperVState.Starting:
                    state = ComputeSystemState.Starting;
                    break;

                case HyperVState.NotApplicable:
                case HyperVState.InTest:
                case HyperVState.Deferred:
                case HyperVState.Quiesce:
                default:
                    state = ComputeSystemState.Unknown;
                    break;
            }

            return state;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Could not get VM state.", ex);
            return ComputeSystemState.Unknown;
        }
    }

    public Windows.Foundation.IAsyncOperation<ComputeSystemState> GetState(string? options)
    {
        try
        {
            return Task.Run(() => GetVmState()).AsAsyncOperation();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Could not get Hyper-V state (async).", ex);
            throw;
        }
    }

    public Windows.Foundation.IAsyncOperation<string> Start(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> ShutDown(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Terminate(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Delete(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Save(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Pause(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Resume(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> CreateSnapshot(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> ApplySnapshot(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> DeleteSnapshot(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> GetProperties(string options)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> ModifyProperties(string properties)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> Connect(string properties)
    {
        throw new NotImplementedException();
    }

    public Windows.Foundation.IAsyncOperation<string> ApplyConfiguration(string configuration)
    {
        throw new NotImplementedException();
    }
}

internal class HyperVWidget : CoreWidget, IDisposable
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(HyperVWidget);

    private readonly IComputeSystemProvider hyperVProvider = new HyperVProvider();

    private IEnumerable<IComputeSystem>? VmCollection { get; set; }

    public HyperVWidget()
        : base()
    {
    }

    // From debugging this method is called multiple times concurrently. Likely needs synchronization for ContentData.
    // Or better way to refresh widget view.
    public async override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Hyper-V VMs state");

        try
        {
            VmCollection = await hyperVProvider.GetComputeSystemsAsync(null, null);

            var vmData = new JsonObject();

            if (VmCollection.Any())
            {
                var vmArray = new JsonArray();
                foreach (var vm in VmCollection)
                {
                    var row = new JsonObject();
                    row.Add("name", vm.Name);

                    var state = await vm.GetState(null);
                    row.Add("state", state.ToString());
                    vmArray.Add(row);
                }

                vmData.Add("items", vmArray);
            }

            DataState = WidgetDataState.Okay;
            ContentData = vmData.ToJsonString();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving Hyper-V VMs data.", ex);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\HyperVTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\HyperVTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,

            // In case of unknown state default to empty data
            _ => EmptyJson,
        };
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        ComputeSystemOperation verb;
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {actionInvokedArgs.Verb}");

        try
        {
            verb = Enum.Parse<ComputeSystemOperation>(actionInvokedArgs.Verb);
        }
        catch (Exception ex)
        {
            // Invalid verb.
            Log.Logger()?.ReportError($"Unknown ComputeSystemOperation verb: {actionInvokedArgs.Verb}", ex);
            return;
        }

        // TODO: Start/Stop/Suspend/Resume
    }

    protected override void SetActive()
    {
        // Create VM objects. Read VM state.
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Content;

        if (ContentData == EmptyJson)
        {
            LoadContentData();
        }

        LogCurrentState();
        UpdateWidget();
    }

    protected override void SetInactive()
    {
        // What to do?
        ActivityState = WidgetActivityState.Inactive;

        LogCurrentState();
    }

    protected override void SetDeleted()
    {
        // What to do?
        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
    }

    public void Dispose()
    {
        // Destroy VM data.
    }
}
