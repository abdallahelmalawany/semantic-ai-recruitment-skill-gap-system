using EtherApp.Controllers.Base;
using EtherApp.Data.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtherApp.Controllers
{
    public class JobMatchController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IInterestService _interestService;

        public JobMatchController(IHttpClientFactory httpClientFactory, IInterestService interestService)
        {
            _httpClientFactory = httpClientFactory;
            _interestService = interestService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Skills = "";
            return View(new List<JobMatchResult>());
        }

        [HttpPost]
        public async Task<IActionResult> Index(string skillInput)
        {
            var userId = GetUserId() ?? 0;
            if (userId == 0)
                return RedirectToAction("Login", "Authentication");

            if (string.IsNullOrWhiteSpace(skillInput))
            {
                var userInterests = await _interestService.GetUserInterestsAsync(userId);
                skillInput = string.Join(", ", userInterests.Select(i => i.Name + " " + i.Keywords));
            }

            var matches = await GetJobMatchesAsync(skillInput);

            ViewBag.Skills = skillInput;
            return View(matches);
        }

        private async Task<List<JobMatchResult>> GetJobMatchesAsync(string skills)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.PostAsJsonAsync(
                "http://localhost:8000/match",
                new { skills });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"API failed: {(int)response.StatusCode} - {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<MatchResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Matches ?? new List<JobMatchResult>();
        }
    }

    public class MatchResponse
    {
        public List<JobMatchResult> Matches { get; set; } = new();
    }

    public class JobMatchResult
    {
        public string Job_Title { get; set; } = string.Empty;
        public double Match_Score { get; set; }
        public string Skills { get; set; } = string.Empty;
    }
}