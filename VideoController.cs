using LearningManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LearningManagementSystem.Controllers
{
    [Authorize]
    public class VideoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<CourseListController> _logger;

        public VideoController(IConfiguration configuration, ILogger<CourseListController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }     


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CoursePlayer(int courseId)
        {
            CourseMediaList courseMediaList = new CourseMediaList();
            try
            {
                DataTable dt = new DataTable();
                var UserId = HttpContext.Session.GetInt32("UserId");
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "lmsGetCourseVideos";
                    cmd.Parameters.AddWithValue("@ICourseDetailsId", courseId);
                    cmd.Parameters.AddWithValue("@IUserId", UserId);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
                if(dt.Rows.Count > 0)
                {
                    List< CourseVideoDetails > videoList = new List< CourseVideoDetails >();
                    for(int i = 0; i < dt.Rows.Count; i++)
                    {
                        CourseVideoDetails vdo = new CourseVideoDetails();
                        DataRow row = dt.Rows[i];

                        vdo.CourseMediaId = Convert.ToInt32(row["CourseMediaId"]);   
                        vdo.Title = row["Title"]?.ToString() ?? "";                
                        vdo.Poster = row["PosterLink"]?.ToString() ?? "";           
                        vdo.Source = row["VideoLink"]?.ToString() ?? "";            
                        if (row["IsCompleted"] == DBNull.Value)
                        {
                            vdo.IsChecked = false;
                        }
                        else
                        {
                            vdo.IsChecked = Convert.ToBoolean(row["IsCompleted"]);
                        }
                        vdo.UserNote = row["UserGivenNotes"].ToString() ?? "";
                        vdo.ModifiedOn = row["ModifiedOn"] == DBNull.Value? (DateTime?)null : Convert.ToDateTime(row["ModifiedOn"]);

                        if(i == 0)
                        {
                            vdo.IsDisabled = false;
                        }
                        else
                        {
                            vdo.IsDisabled = true;

                        }

                        videoList.Add(vdo);
                    }
                    courseMediaList.Videos = videoList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oops something went wrong!!");
                throw;
            }
            return View(courseMediaList);
        }

    }
}
