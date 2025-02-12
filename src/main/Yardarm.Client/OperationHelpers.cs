﻿using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Yardarm.Client
{
    internal static class OperationHelpers
    {
        public static string AddQueryParameters(string path, IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var builder = new StringBuilder(path);

            var first = true;
            foreach (var parameter in parameters)
            {
                if (parameter.Value != null)
                {
                    if (first)
                    {
                        builder.Append('?');
                        first = false;
                    }
                    else
                    {
                        builder.Append('&');
                    }

                    builder.AppendFormat("{0}={1}", Uri.EscapeDataString(parameter.Key),
                        Uri.EscapeDataString(parameter.Value.ToString() ?? ""));
                }
            }

            return builder.ToString();
        }
    }
}
