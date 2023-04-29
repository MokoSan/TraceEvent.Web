using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Etlx = Microsoft.Diagnostics.Tracing.Etlx;

namespace TraceEvent.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraceEventController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        public TraceEventController([FromServices] IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            return Ok("Up and running!");
        }

        [HttpPost(Name = "PostTraceDetails")]
        public async Task<ActionResult> Post(IFormFile file)
        {
            if (!file.FileName.Contains(".etl"))
            {
                return BadRequest("A non .etl file was passed!");
            }

            string uploads = Path.Combine(_environment.WebRootPath, file.FileName);
            using (Stream fileStream = new FileStream(uploads, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            string path = uploads;
            if (Path.GetExtension(file.FileName).Contains(".zip")) 
            {
                ZippedETLReader reader = new(uploads);
                reader.UnpackArchive();
                path = reader.EtlFileName;
            }

            Etlx.TraceLog traceLog = Etlx.TraceLog.OpenOrConvert(path);
            var eventSource = traceLog.Events.GetSource();
            eventSource.NeedLoadedDotNetRuntimes();
            eventSource.Process();
            List<GCInfo> gcInfos = new();
            foreach (var p in eventSource.Processes())
            {
                var managedProcess = p.LoadedDotNetRuntime();
                if (managedProcess == null || managedProcess.GC == null || managedProcess.GC.GCs == null || managedProcess.GC.GCs.Count <= 0)
                {
                    continue;
                }
                var stats = managedProcess.GC.Stats();

                gcInfos.Add(
                    new GCInfo
                    {
                        Summary = $"For {p.Name}, the % Pause Time in GC is: {stats.GetGCPauseTimePercentage()}% and the Total Allocations are {stats.TotalAllocatedMB}",
                        ProcessName = p.Name,
                        TotalAllocationsMB = stats.TotalAllocatedMB,
                        GCPauseTimePercentage = stats.GetGCPauseTimePercentage(),
                        TotalGCPauseTimeMSec = stats.TotalPauseTimeMSec
                    });
            }

            // TODO: Clean up file.

            return Ok(gcInfos);
        }
    }
}
