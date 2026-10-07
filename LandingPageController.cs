using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using LearningManagementSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace LearningManagementSystem.Controllers
{
    
    public class LandingPageController : Controller
    {
        private readonly SignInManager<LMSUser> _signInManager;
        private readonly string _connectionString;
        private readonly ILogger<LandingPageController> _logger;
        public LandingPageController
            (SignInManager<LMSUser> signInManager
            , IConfiguration configuration,
             ILogger<LandingPageController> logger
            )
        {
            _signInManager = signInManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LandingPage(string Type)
        {
            CourseCategory category = new CourseCategory();
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var email = HttpContext.Session.GetString("Email");
                if (email != null)
                {
                    GetUserDataByEmail(email); 
                }
            }
            try
            {
                DataTable dt = new DataTable();
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand command = con.CreateCommand();
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "GetAllCourseCategories";
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(dt);
                }

                List<CourseCategoryList> courseList = new List<CourseCategoryList>();
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        CourseCategoryList course = new CourseCategoryList();
                        course.CourseMasterId = Convert.ToInt32(dt.Rows[i]["CourseMasterId"]);
                        course.NoOfCourses = Convert.ToInt32(dt.Rows[i]["NoOfCourses"]);
                        course.CourseMasterType = (dt.Rows[i]["CourseMasterType"])?.ToString() ?? "";

                        courseList.Add(course);
                    }
                    category.CourseCategoryList = courseList;
                    var type = Type==null ? "Popular" : Type;
                    category.popularOrDemandingCourseList = GetCourseList(type);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oops");
                throw;
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LandingPage", "LandingPage");
        }

        [HttpGet]
        public IActionResult IsUserLoggedIn()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Json(new { loggedIn = true });
            }
            return Json(new { loggedIn = false });
        }

        public void GetUserDataByEmail(string email)
        {
            try
            {
                DataTable dt = new DataTable();
                if (email != null && email != "")
                {
                    using (SqlConnection con = new SqlConnection(_connectionString))
                    {
                        con.Open();
                        SqlCommand command = con.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "lmsGetUserDetailsByEmailId";
                        command.Parameters.AddWithValue("@IEmailId", email);
                        SqlDataAdapter da = new SqlDataAdapter(command);
                        da.Fill(dt);
                    }
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        var userId = Convert.ToInt32(dt.Rows[0]["UserId"]);
                        HttpContext.Session.SetInt32("UserId", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserDataByEmail for email: {Email}", email);
                throw;
            }


        }


        public List<CourseInfo> GetCourseList(string Type)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlCommand command = con.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "lmsGetPopularOrDemandingCourses";
                command.Parameters.AddWithValue("@IType", Type);
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
            }
            List<CourseInfo> list = new List<CourseInfo>();
            if (dt != null && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    CourseInfo course = new CourseInfo();

                    course.CourseDetailsId = Convert.ToInt32(dt.Rows[i]["CourseDetailsId"]);
                    course.CourseName = dt.Rows[i]["CourseName"].ToString() ?? "";
                    course.Duration = dt.Rows[i]["Duration"].ToString() ?? "";
                    course.CourseProvider = dt.Rows[i]["CourseProvider"].ToString() ?? "";
                    course.CourseFees = Convert.ToDecimal(dt.Rows[i]["CourseFees"]);
                    course.NoOfEnrollment = Convert.ToInt32(dt.Rows[i]["NoOfEnrollment"]);
                    course.CourseImage = dt.Rows[i]["CourseImage"].ToString() ?? "";

                    list.Add(course);
                }
            }
            return list;
        }

    }
}
