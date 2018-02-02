// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Headers;

namespace System.Net.Http
{
    internal static class HttpUtilities
    {
        internal static Version DefaultRequestVersion =>
#if uap
            HttpVersionInternal.Version20;
#else
            HttpVersionInternal.Version11;
#endif
        internal static Version DefaultResponseVersion => HttpVersionInternal.Version11;

        internal static bool IsHttpUri(Uri uri)
        {
            Debug.Assert(uri != null);
            return IsSupportedScheme(uri.Scheme);
        }

        internal static bool IsSupportedScheme(string scheme) =>
            IsSupportedNonSecureScheme(scheme) ||
            IsSupportedSecureScheme(scheme);

        internal static bool IsSupportedNonSecureScheme(string scheme) =>
            string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase);

        internal static bool IsSupportedSecureScheme(string scheme) =>
            string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase);

        // Always specify TaskScheduler.Default to prevent us from using a user defined TaskScheduler.Current.
        //
        // Since we're not doing any CPU and/or I/O intensive operations, continue on the same thread.
        // This results in better performance since the continuation task doesn't get scheduled by the
        // scheduler and there are no context switches required.
        internal static Task ContinueWithStandard<T>(this Task<T> task, object state, Action<Task<T>, object> continuation)
        {
            return task.ContinueWith(continuation, state, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        internal static string GetSslHostName(HttpRequestMessage request)
        {
            Uri uri = request.RequestUri;

            if (!HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                // Not using SSL.
                return null;
            }


            // Get the appropriate host name to use for the SSL connection, allowing a host header to override.
            string host = request.Headers.Host;
            if (host == null)
            {
                // No host header, use the host from the Uri.
                host = uri.IdnHost;
            }
            else
            {
                // There is a host header.  Use it, but first see if we need to trim off a port.
                int colonPos = host.IndexOf(':');
                if (colonPos >= 0)
                {
                    // There is colon, which could either be a port separator or a separator in
                    // an IPv6 address.  See if this is an IPv6 address; if it's not, use everything
                    // before the colon as the host name, and if it is, use everything before the last
                    // colon iff the last colon is after the end of the IPv6 address (otherwise it's a
                    // part of the address).
                    int ipV6AddressEnd = host.IndexOf(']');
                    if (ipV6AddressEnd == -1)
                    {
                        host = host.Substring(0, colonPos);
                    }
                    else
                    {
                        colonPos = host.LastIndexOf(':');
                        if (colonPos > ipV6AddressEnd)
                        {
                            host = host.Substring(0, colonPos);
                        }
                    }
                }
            }
            return host;
        }
    }
}
