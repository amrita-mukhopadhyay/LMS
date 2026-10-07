using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LearningManagementSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LearningManagementSystem.Controllers
{
    [Authorize]
    public class CourseListController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<CourseListController> _logger;

        public CourseListController(IConfiguration configuration, ILogger<CourseListController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(int Id)
        {
            CourseListPageLoad model = new CourseListPageLoad();
            model.courseMasterList = new List<CourseMaster>();
            model.skillsList = new List<Skills>();

            try
            {

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand cmd1 = new SqlCommand("lmsGetAllCourseType", con);
                    cmd1.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da1 = new SqlDataAdapter(cmd1);
                    DataTable dt1 = new DataTable();
                    da1.Fill(dt1);

                    foreach (DataRow row in dt1.Rows)
                    {
                        model.courseMasterList.Add(new CourseMaster
                        {
                            CourseMasterId = Convert.ToInt32(row["CourseMasterId"]),
                            CourseMasterType = row["CourseMasterType"].ToString()
                        });
                    }

                    SqlCommand cmd2 = new SqlCommand("lmsGetAllSkills", con);
                    cmd2.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da2 = new SqlDataAdapter(cmd2);
                    DataTable dt2 = new DataTable();
                    da2.Fill(dt2);

                    foreach (DataRow row in dt2.Rows)
                    {
                        model.skillsList.Add(new Skills
                        {
                            SkillId = Convert.ToInt32(row["SkillId"]),
                            SkillName = row["SkillName"].ToString()
                        });
                    }
                }

                var courseList = GetCoursesByMasterId(Id);
                ViewBag.CourseList = courseList;
                ViewBag.SelectedCategoryId = Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Index for Id: {Id}", Id);
                throw;
            }
            return View(model);
        }

        public List<CourseInfo> GetCoursesByMasterId(int id)
        {
            List<CourseInfo> courses = new List<CourseInfo>();
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlCommand command = con.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "lsmGetCoursesById";
                command.Parameters.AddWithValue("@ICourseMasterId", id);
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
            }

            foreach (DataRow row in dt.Rows)
            {
                courses.Add(new CourseInfo()
                {
                    CourseDetailsId = Convert.ToInt32(row["CourseDetailsId"]),
                    CourseName = row["CourseName"].ToString(),
                    Duration = row["Duration"].ToString(),
                    CourseProvider = row["CourseProvider"].ToString(),
                    CourseFees = Convert.ToDecimal(row["CourseFees"]),
                    NoOfEnrollment = Convert.ToInt32(row["NoOfEnrollment"]),
                    CourseStatusName = row["CourseStatusName"].ToString(),
                    CourseImage = row["CourseImage"].ToString()
                });
            }

            return courses;
        }

        private List<CourseInfo> GetFilteredCourses(
            int? categoryId,
            string courseName,
            string provider,
            string status,
            string courseTypesCsv,
            string skillsCsv
        )
        {
            bool hasAnyFilter = !string.IsNullOrWhiteSpace(courseName)
                                || !string.IsNullOrWhiteSpace(provider)
                                || !string.IsNullOrWhiteSpace(status)
                                || !string.IsNullOrWhiteSpace(courseTypesCsv)
                                || !string.IsNullOrWhiteSpace(skillsCsv);

            if (!hasAnyFilter)
            {
                return GetCoursesByMasterId(categoryId ?? 0);
            }
            List<CourseInfo> courses = new List<CourseInfo>();
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "lsmFilterCourses";

                cmd.Parameters.AddWithValue("@CourseName", (object)(courseName ?? ""));
                cmd.Parameters.AddWithValue("@Provider", (object)(provider ?? ""));
                cmd.Parameters.AddWithValue("@Status", (object)(status ?? ""));
                cmd.Parameters.AddWithValue("@CourseTypesCSV", (object)(courseTypesCsv ?? ""));
                cmd.Parameters.AddWithValue("@SkillsCSV", (object)(skillsCsv ?? ""));

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            foreach (DataRow row in dt.Rows)
            {
                courses.Add(new CourseInfo()
                {
                    CourseDetailsId = Convert.ToInt32(row["CourseDetailsId"]),
                    CourseName = row["CourseName"].ToString(),
                    Duration = row["Duration"].ToString(),
                    CourseProvider = row["CourseProvider"].ToString(),
                    CourseFees = Convert.ToDecimal(row["CourseFees"]),
                    NoOfEnrollment = Convert.ToInt32(row["NoOfEnrollment"]),
                    CourseStatusName = row["CourseStatusName"].ToString()
                });
            }

            return courses;
        }


        [HttpGet]
        public IActionResult GetPagedCourses(
            int id = 0,
            int page = 1,
            int pageSize = 6,
            string courseName = "",
            string provider = "",
            string status = "",
            string courseTypes = "",
            string skills = ""
        )
        {
            try
            {
                var allCourses = GetFilteredCourses(id, courseName, provider, status, courseTypes, skills);

                int totalCount = allCourses.Count;
                var paged = allCourses
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.SelectedCategoryId = id;
                ViewBag.CourseNameFilter = courseName ?? "";
                ViewBag.ProviderFilter = provider ?? "";
                ViewBag.StatusFilter = status ?? "";
                ViewBag.CourseTypesFilter = courseTypes ?? "";
                ViewBag.SkillsFilter = skills ?? "";

                return PartialView("_CourseListPartial", paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPagedCourses");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost]
        public IActionResult FilterCourses(
            string courseName,
            string provider,
            string status,
            string courseTypes,
            string skills,
            int id = 0
        )
        {

            return GetPagedCourses(id: id, page: 1, pageSize: 6,
                courseName: courseName ?? "",
                provider: provider ?? "",
                status: status ?? "",
                courseTypes: courseTypes ?? "",
                skills: skills ?? "");
        }


        [HttpGet]
        public IActionResult GetCourseDetails(int id)
        {
            CourseDetailsByID course = new CourseDetailsByID();
            course.Skills = new List<Skills>();
            course.CourseSpecializationList = new List<CourseSpecialization>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("lmsGetCourseDetailsById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ICourseDetailsById", id);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();

                conn.Open();
                da.Fill(ds);
                conn.Close();

                // ------------------------ TABLE 0 : Course Details ------------------------
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];

                    course.email = HttpContext.Session.GetString("Email") ?? "";
                    course.CourseDetailsId = Convert.ToInt32(row["CourseDetailsId"]);
                    course.Duration = Convert.ToInt32(row["Duration"]);
                    course.CourseName = row["CourseName"].ToString() ?? "";
                    course.CourseProvider = row["CourseProvider"].ToString() ?? "";
                    course.IsFree = Convert.ToBoolean(row["IsFree"]);
                    course.CourseFees = row["CourseFees"] != DBNull.Value
                        ? Convert.ToDecimal(row["CourseFees"])
                        : 0;
                }
                else
                {
                    return null; // no course found
                }

                // ------------------------ TABLE 1 : Skills ------------------------
                if (ds.Tables.Count > 1)
                {
                    DataTable skillTable = ds.Tables[1];

                    for (int i = 0; i < skillTable.Rows.Count; i++)
                    {
                        DataRow row = skillTable.Rows[i];
                        course.Skills.Add(new Skills
                        {
                            SkillId = Convert.ToInt32(row["SkillId"]),
                            SkillName = row["SkillName"].ToString()
                        });
                    }
                }

                // ------------------------ TABLE 2 : Specializations ------------------------
                if (ds.Tables.Count > 2)
                {
                    DataTable specTable = ds.Tables[2];

                    for (int i = 0; i < specTable.Rows.Count; i++)
                    {
                        DataRow row = specTable.Rows[i];
                        course.CourseSpecializationList.Add(new CourseSpecialization
                        {
                            SpecializationId = Convert.ToInt32(row["SpecializationId"]),
                            Specialization = row["Specialization"].ToString()
                        });
                    }
                }

                // ------------------------ TABLE 3 : Related course list ------------------------
                if (ds.Tables.Count > 3)
                {
                    DataTable relatedTable = ds.Tables[3];
                    List<RelatedCourses> related = new List<RelatedCourses>();
                    for (int i = 0; i < relatedTable.Rows.Count; i++)
                    {
                        RelatedCourses rtl = new RelatedCourses();
                        DataRow row = relatedTable.Rows[i];

                        //course.RelatedCourses.Add(new RelatedCourses
                        //{
                        rtl.CourseDetailsId = Convert.ToInt32(row["CourseDetailsId"]);
                        rtl.CourseName = row["CourseName"].ToString();
                        rtl.CourseImage = row["CourseImage"].ToString();
                        rtl.CourseProvider = row["CourseProvider"].ToString();
                        //});

                        related.Add(rtl);
                    }
                    course.RelatedCourses = related;
                }
            }
            return View(course);
        }

        [HttpPost]
        public Tuple<int, string> courseEnrollment( int courseDetailsId, int isPaid)
        {
            // default: failure
            var response = Tuple.Create(0, "Something went wrong. Please try again.");
            var UserId = HttpContext.Session.GetInt32("UserId");
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("lmsCourseEnrollMent", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IUserId", UserId);
                    cmd.Parameters.AddWithValue("@CourseDetailsId", courseDetailsId);
                    cmd.Parameters.AddWithValue("@IsPaid", isPaid);

                    var ooIdParam = new SqlParameter("@OOId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(ooIdParam);

                    var oMsgParam = new SqlParameter("@OMsg", SqlDbType.NVarChar, -1)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(oMsgParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    long newId = 0;
                    if (ooIdParam.Value != DBNull.Value)
                        newId = Convert.ToInt64(ooIdParam.Value);

                    string msg = oMsgParam.Value == DBNull.Value ? "Enrollment completed." : oMsgParam.Value.ToString();

                    int status = newId > 0 ? 1 : 0;

                    response = Tuple.Create(status, msg);
                }
            }
            catch (Exception ex)
            {
                // log ex
                response = Tuple.Create(0, "Error during enrollment.");
            }

            return response;
        }
    }
}
