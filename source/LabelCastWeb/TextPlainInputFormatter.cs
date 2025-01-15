using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace LabelCastWeb
{
    /// <summary>
    /// This class is to align content-types of POST requests with what ASP.NET expects.
    /// See references below.
    /// </summary>
    public class TextPlainInputFormatter : TextInputFormatter
    {
        public TextPlainInputFormatter()
        {
            SupportedMediaTypes.Add("text/plain");
            SupportedMediaTypes.Add("text/csv");
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }

        // Async version - ASP.Net Core 3.0 and later

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding encoding)
        {
            var request = context.HttpContext.Request;
            string? data = null;
            using (var streamReader = new StreamReader(request.Body, encoding))
            {
                data = await streamReader.ReadToEndAsync();
                return await InputFormatterResult.SuccessAsync(data);
            }
        }

    }



    // Source: 
    // http://stackoverflow.com/questions/41798814/asp-net-core-api-post-parameter-is-always-null
    // https://github.com/aspnet/Mvc/issues/5137    
    // Update for ASP.Net Core 3.0 - for async
    // https://weblog.west-wind.com/posts/2017/Sep/14/Accepting-Raw-Request-Body-Content-in-ASPNET-Core-API-Controllers


}
