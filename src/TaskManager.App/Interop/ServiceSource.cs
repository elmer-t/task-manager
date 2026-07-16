using System.Runtime.InteropServices;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Services;

namespace TaskManager.App.Interop;

/// <summary>
/// Enumerates Windows services from the Service Control Manager (spec §4): names, states,
/// and hosting PID via EnumServicesStatusEx(SC_ENUM_PROCESS_INFO); the static Description
/// column via QueryServiceConfig2, read once per service and cached so it stays off the
/// hot path (spec §5). A service the caller can't open is simply skipped — never an error
/// (spec §4). View-only; nothing here starts or stops a service (spec §2).
/// </summary>
internal sealed class ServiceSource : IServiceSource
{
    private const int InitialBufferBytes = 64 * 1024;

    // Descriptions are static; cache by service key so we query each at most once.
    private readonly Dictionary<string, string?> _descriptionCache = new(StringComparer.OrdinalIgnoreCase);

    public unsafe IReadOnlyList<ServiceSample> Sample()
    {
        using var scm = PInvoke.OpenSCManager(
            lpMachineName: (string?)null,
            lpDatabaseName: (string?)null,
            dwDesiredAccess: PInvoke.SC_MANAGER_ENUMERATE_SERVICE);

        if (scm.IsInvalid)
        {
            return Array.Empty<ServiceSample>();
        }

        var results = new List<ServiceSample>();
        byte[] buffer = new byte[InitialBufferBytes];
        uint resumeHandle = 0;

        while (true)
        {
            bool ok = PInvoke.EnumServicesStatusEx(
                scm,
                SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
                ENUM_SERVICE_TYPE.SERVICE_WIN32,
                ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
                buffer,
                out uint bytesNeeded,
                out uint servicesReturned,
                &resumeHandle,
                pszGroupName: null!);

            if (ok)
            {
                Parse(scm, buffer, servicesReturned, results);
                break;
            }

            var error = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (error != WIN32_ERROR.ERROR_MORE_DATA)
            {
                break; // Can't enumerate further; return what we have.
            }

            // Partial page: consume what fit, grow the buffer, keep paging via resumeHandle.
            if (servicesReturned > 0)
            {
                Parse(scm, buffer, servicesReturned, results);
            }

            if (bytesNeeded > buffer.Length)
            {
                buffer = new byte[bytesNeeded];
            }
        }

        return results;
    }

    private unsafe void Parse(SafeHandle scm, byte[] buffer, uint count, List<ServiceSample> results)
    {
        int stride = sizeof(ENUM_SERVICE_STATUS_PROCESSW);
        fixed (byte* basePtr = buffer)
        {
            for (uint i = 0; i < count; i++)
            {
                var entry = (ENUM_SERVICE_STATUS_PROCESSW*)(basePtr + (i * stride));

                string serviceName = entry->lpServiceName.ToString() ?? string.Empty;
                string displayName = entry->lpDisplayName.ToString() ?? serviceName;

                SERVICE_STATUS_PROCESS status = entry->ServiceStatusProcess;
                ServiceStatus runState = status.dwCurrentState == SERVICE_STATUS_CURRENT_STATE.SERVICE_RUNNING
                    ? ServiceStatus.Running
                    : ServiceStatus.Stopped;
                int? hostPid = status.dwProcessId != 0 ? (int)status.dwProcessId : null;

                string? description = GetDescription(scm, serviceName);

                results.Add(new ServiceSample(serviceName, displayName, description, runState, hostPid));
            }
        }
    }

    private string? GetDescription(SafeHandle scm, string serviceName)
    {
        if (_descriptionCache.TryGetValue(serviceName, out string? cached))
        {
            return cached;
        }

        string? description = ReadDescription(scm, serviceName);
        _descriptionCache[serviceName] = description;
        return description;
    }

    private static unsafe string? ReadDescription(SafeHandle scm, string serviceName)
    {
        using var service = PInvoke.OpenService(scm, serviceName, PInvoke.SERVICE_QUERY_CONFIG);
        if (service.IsInvalid)
        {
            return null; // Blank Description cell; the row still appears (spec §4).
        }

        // Probe for the required size, then read the SERVICE_DESCRIPTIONW blob.
        PInvoke.QueryServiceConfig2W(service, SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION, Span<byte>.Empty, out uint needed);
        if (needed == 0)
        {
            return null;
        }

        byte[] buffer = new byte[needed];
        if (!PInvoke.QueryServiceConfig2W(service, SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION, buffer, out _))
        {
            return null;
        }

        fixed (byte* ptr = buffer)
        {
            var description = (SERVICE_DESCRIPTIONW*)ptr;
            string? text = description->lpDescription.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
