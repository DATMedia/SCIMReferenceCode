using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace PipelineLogger
{
    internal sealed class HttpRequestSnapshot
    {
        internal Dictionary<string, string> Cookies { get; set; }

        internal string Method { get; set; }
        internal PathString Path { get; set; }
        internal IDictionary<string, StringValues> Headers { get; set; }
    }

    internal sealed class HttpResponseSnapshot
    {
        public int StatusCode { get; set; }
        internal IDictionary<string, StringValues> Headers { get; set; }

        public long BodyLength { get; set; }
    }

    internal sealed class BeforeAndAfter<T>
    {
        public T Before;
        public T After;
    }

    internal sealed class DictionaryDifference<T>
    {
        public IEnumerable<KeyValuePair<string, T>> Removed;
        public IEnumerable<KeyValuePair<string, T>> Added;
        public IEnumerable<KeyValuePair<string, BeforeAndAfter<T>>> Modified;

        internal bool Empty => !(Removed?.Any() ?? false) && !(Added?.Any() ?? false) && !(Modified?.Any() ?? false);
    }

    internal sealed class HttpRequestDifference
    {
        internal DictionaryDifference<StringValues> Headers;
        internal bool Empty => Headers.Empty;
    }

    internal sealed class HttpResponseDifference
    {
        internal DictionaryDifference<StringValues> Headers;

        internal BeforeAndAfter<int> StatusCode;

        internal BeforeAndAfter<long> BodyLength;
        internal bool Empty => Headers.Empty && StatusCode == default(BeforeAndAfter<int>) && BodyLength == default(BeforeAndAfter<long>);
    }

    internal sealed class HttpContextDifference
    {
        internal HttpRequestDifference Request;
        internal HttpResponseDifference Response;

        public bool ChangedUser { get; internal set; }
        public ClaimsPrincipal User { get; internal set; }
        internal bool Empty => Request.Empty && Response.Empty && !ChangedUser;
    }

    internal sealed class HttpContextSnapshot
    {
        internal HttpRequestSnapshot Request
        {
            get;
            private set;
        }

        internal HttpResponseSnapshot Response
        {
            get;
            private set;
        }

        internal ClaimsPrincipal User
        {
            get;
            private set;
        }

        internal static HttpContextSnapshot TakeSnapshot(HttpContext httpContext)
        {
            return new HttpContextSnapshot
            {
                Request = new HttpRequestSnapshot
                {
                    Method = httpContext.Request.Method,
                    Path = httpContext.Request.Path,
                    Headers = new Dictionary<string, StringValues>(httpContext.Request.Headers),
                    Cookies = httpContext.Request.Cookies.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                },
                User = httpContext.User,
                Response = new HttpResponseSnapshot
                {
                    Headers = new Dictionary<string, StringValues>(httpContext.Response.Headers),
                    StatusCode = httpContext.Response.StatusCode,
                    BodyLength = httpContext.Response.Body.Length,
                },
            };
        }

        private static DictionaryDifference<StringValues> CompareHeaders(IDictionary<string, StringValues> before, IDictionary<string, StringValues> after)
        {
            var added = new List<KeyValuePair<string, StringValues>>();
            var removed = new List<KeyValuePair<string, StringValues>>();
            var modified = new List<KeyValuePair<string, BeforeAndAfter<StringValues>>>();
            foreach (var kvp in before)
            {
                if (after.TryGetValue(kvp.Key, out StringValues correspondingAfterValue))
                {
                    if (kvp.Value != correspondingAfterValue)
                    {
                        modified.Add(
                            new KeyValuePair<string, BeforeAndAfter<StringValues>>(
                                kvp.Key,
                                new BeforeAndAfter<StringValues> { Before = kvp.Value, After = correspondingAfterValue }
                            )
                        );
                    }
                }
                else
                {
                    removed.Add(kvp);
                }
            }
            foreach (var kfp in after)
            {
                if (!(before.Keys.Contains(kfp.Key)))
                {
                    added.Add(kfp);
                }
            }
            return new DictionaryDifference<StringValues>
            {
                Added = added,
                Modified = modified,
                Removed = removed,
            };
        }

        private static BeforeAndAfter<T> ComparePrimitives<T>(T before, T after)
        {
            if (Object.ReferenceEquals(before, after))
            {
                return null;
            }
            if (object.ReferenceEquals(before, null) || !before.Equals(after))
            {
                return new BeforeAndAfter<T> { Before = before, After = after };
            }
            else
            {
                return null;
            }
        }

        internal static HttpContextDifference Compare(HttpContextSnapshot before, HttpContextSnapshot after)
        {
            return new HttpContextDifference
            {
                Request = new HttpRequestDifference
                {
                    Headers = CompareHeaders(before.Request.Headers, after.Request.Headers),
                },
                Response = new HttpResponseDifference
                {
                    Headers = CompareHeaders(before.Response.Headers, after.Response.Headers),
                    StatusCode = ComparePrimitives(before.Response.StatusCode, after.Response.StatusCode),
                    BodyLength = ComparePrimitives(before.Response.BodyLength, after.Response.BodyLength),
                },
                ChangedUser = before.User != after.User,
                User = after.User,
            };
        }
    }
}
