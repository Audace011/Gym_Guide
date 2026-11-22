using System.Collections.Generic;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Tests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RunTestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RunTestsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TestResult> Results { get; set; } = new();

        public async Task OnGetAsync()
        {
            Results = await UnitTestsRunner.RunAllTestsAsync(_context);
        }
    }
}
