using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace PipelineLogger
{
    class StringFormatter
    {
        private static (bool success, string result) TryParseAuthorizationHeader(string s)
        {
            var regex = new Regex(@"^Basic ([A-Za-z0-9+/=\-_]+)$");
            var match = regex.Match(s);
            if (!match.Success)
            {
                return (false, null);
            }
            string encodedString = match.Groups[1].Value;
            byte[] data = Convert.FromBase64String(encodedString);
            string decodedString = Encoding.UTF8.GetString(data);
            return (
                    true,
                    $"Basic <from Base64 string>{Format(decodedString)}"
                );
        }

        static class JsonRegularExpressions
        {
            internal readonly static Regex BeginsWithInvalidCharacter = new Regex(@"^\s*\d*[\*\w</,\.]");
        }
        // Probably not a completely working function
        // We have this so that the debug output
        // isn't full of "Caught exception of type Newtonsoft.JsonException
        private static bool LooksLikeValidJson(string s)
        {
            // If it starts with an alphabetic character it fails
            if (JsonRegularExpressions.BeginsWithInvalidCharacter.IsMatch(s))
            {
                return false;
            }
            return true;
        }

        private static (bool success, string result) TryParseJson(string s)
        {
            if (!LooksLikeValidJson(s))
            {
                return (false, default(string));
            }
            try
            {
                dynamic asObject = JsonConvert.DeserializeObject(s);
                return (
                            true,
                            JsonConvert.SerializeObject(asObject, Formatting.Indented)
                        );
            }
            catch (Exception)
            {
                return (false, default(string));
            }
        }

        private static (bool success, string result) TryParseLargeText(string s)
        {
            if (s.Length > 1000)
            {
                return (true, $"{s.Length} characters");
            }
            else
            {
                return (false, default(string));
            }
        }

        public static string Format(string value)
        {
            var parsers = new Func<string, (bool success, string result)>[]
            {
                TryParseJson,
                TryParseAuthorizationHeader,
                TryParseLargeText,
            };
            foreach (var parse in parsers)
            {
                (bool success, string result) = parse(value);
                if (success)
                {
                    return result;
                }
            }
            return value;
        }

        public static string Format(StringValues stringValues)
        {
            IEnumerable<string> formatted = stringValues.Select(sv => Format(sv));
            return string.Concat(formatted, "\n");
        }

        private static string OutputWithHangingIndent(IEnumerable<string> s, int indentLength)
        {
            StringBuilder sb = new StringBuilder();
            if (s.Any())
            {
                sb.Append(s.First());
            }
            foreach (var line in s.Skip(1))
            {
                sb.AppendLine();
                sb.Append($"{"".PadLeft(indentLength)}{line}");
            }
            return sb.ToString();
        }

        public static string GenerateBanner(string bannerMessage)
        {
            var horizontalBorder = string.Empty.PadLeft(bannerMessage.Length + 4, '*');
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            foreach (var s in new[]
            {
                horizontalBorder,
                $"* {bannerMessage} *",
                horizontalBorder,
            })
            {
                sb.AppendLine(s);
            }
            return sb.ToString();
        }

        internal static string FormatBody(string contentType, byte[] body)
        {
            if (body.Length == 0)
            {
                return string.Empty;
            }
            if (contentType == "application/x-www-form-urlencoded")
            {
                var encoding = new System.Text.UTF8Encoding();
                string bodyAsString = encoding.GetString(body);
                var split = bodyAsString.Split('&');
                var keyValuePairs = split.Select(s =>
                {
                    var pair = s.Split('=');
                    return new KeyValuePair<string, StringValues>(Uri.UnescapeDataString(pair[0]), new StringValues(Uri.UnescapeDataString(pair[1])));
                });
                return $"=== {contentType} ===\r\n{Format(keyValuePairs)}";
            }
            else if (contentType == "application/json; charset=UTF-8")
            {
                var encoding = new System.Text.UTF8Encoding();
                string bodyAsString = encoding.GetString(body);
                return Format(bodyAsString);
            }
            else if ((contentType == "text/html; charset=UTF-8") && (body.Length < 2000))
            {
                var encoding = new System.Text.UTF8Encoding();
                string bodyAsString = encoding.GetString(body);
                return bodyAsString;
            }
            return $"=== {contentType} ===\r\n{body.Length} bytes";
        }

        internal static string FormatResponseBody(string contentType, Stream body)
        {
            var posBefore = body.Position;
            body.Seek(0, SeekOrigin.Begin);
            try
            {
                byte[] rawData = new byte[body.Length];
                body.Read(rawData, 0, (int)body.Length);
                return FormatBody(contentType, rawData);
            }
            finally
            {
                body.Seek(posBefore, SeekOrigin.Begin);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ExtractProperties(ClaimsPrincipal user)
        {
            var identityCount = user.Identities.Count();
            yield return new KeyValuePair<string, string>("IdentityCount", $"{identityCount}");
            if (identityCount == 0)
            {
                yield break;
            }
            foreach (var identity in user.Identities)
            {
                bool isAuthenticated = identity.IsAuthenticated;
                yield return new KeyValuePair<string, string>(nameof(identity.IsAuthenticated), $"{isAuthenticated}");

                if (isAuthenticated)
                {
                    yield return new KeyValuePair<string, string>(
                        nameof(identity.AuthenticationType),
                        identity.AuthenticationType
                    );
                    yield return new KeyValuePair<string, string>(nameof(identity.Name), identity.Name);
                    foreach (var claim in identity.Claims)
                    {
                        yield return new KeyValuePair<string, string>($"Claims.{claim.Type}", $"<{claim.ValueType}>{claim.Value}");
                    }
                }
            }
        }

        internal static string Format(ClaimsPrincipal user)
        {
            return Format(ExtractProperties(user));
        }

        internal static string Format(IEnumerable<KeyValuePair<string, string>> dictionary)
        {
            return Format(dictionary.Select(kvp => new KeyValuePair<string, StringValues>(kvp.Key, new StringValues(kvp.Value))));
        }

        internal static string Format(IEnumerable<KeyValuePair<string, StringValues>> dictionary)
        {
            if (!dictionary.Any())
            {
                return "None";
            }
            var maxHeaderLength = dictionary.Select(k => k.Key.Length).Max();

            var allLines = dictionary
                                .Select(kvp => new { label = $"[{kvp.Key.PadRight(maxHeaderLength)}] : ", values = kvp.Value.Select(s => Format(s)) })
                                .Select(_ => $"{_.label}{OutputWithHangingIndent(_.values, _.label.Length)}")
                                .ToArray();
            StringBuilder sb = new StringBuilder();
            foreach (var line in allLines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
