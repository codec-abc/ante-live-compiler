using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

using Newtonsoft.Json;

namespace anteCompilerAPI.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost]
        [Route("play/evaluate.json")]
        public JsonResult Post()
        {
            try
            {
                var stream = new StreamReader(this.Request.Body);
                var body = stream.ReadToEnd();
                var compilePayload = JsonConvert.DeserializeObject<CompilePayload>(body);

                var b64 = Base64Encode(compilePayload.code);

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
                    return new JsonResult(new CompilationResult() { conec = content });
                }
                else
                {
                    return new JsonResult(new CompilationResult() { conec = "unexpected error. Exit code is " + process.ExitCode });
                }
            }
            catch (Exception e)
            {
                return new JsonResult(new CompilationResult() { conec = "error: " + e.Message });
            }
        }

        public static string Base64Encode(string plainText) 
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }

    public class CompilationResult
    {
        public string conec;
    }

    public class CompilePayload
    {
        public string code;
        public bool color;
        public bool separate_output;
    }
}
