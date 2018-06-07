using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

namespace anteCompilerAPI.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost]
        [Route("play/evaluate.json")]
        public ActionResult<string> Post()
        {
            try
            {
                var stream = new StreamReader(this.Request.Body);
                var body = stream.ReadToEnd();

                var b64 = Base64Encode(body);

                Process process = new Process(); 
                process.StartInfo.FileName = "docker";

                process.StartInfo.Arguments = "run --rm ante bash /home/src/run.sh " + b64;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                string content = null;

                process.OutputDataReceived += (sender, args) => 
                {
                    if (content == null)
                    {
                        content = args.Data;
                    }
                    else
                    {
                        content += args.Data;
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit(15000);

                if (content != null)
                {
                    return content;
                }
                else
                {
                    return "unexpected error";
                }
            }
            catch (Exception e)
            {
                return "error: " + e.Message;
            }
        }

        public static string Base64Encode(string plainText) 
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
