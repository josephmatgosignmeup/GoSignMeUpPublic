using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsmu.Service.Models.School;
using Gsmu.Service.Interface.Admin.Global;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.Survey.Entities;
using Gsmu.Service.BusinessLogic.GlobalTools;
using Gsmu.Service.Models.Constants;
using Gsmu.Service.Models.Admin.CourseDashboard;
using Gsmu.Service.Models.Generic;

using Newtonsoft.Json;
using Gsmu.Service.Models.Admin.Global;

namespace Gsmu.Service.BusinessLogic.Global.Settings
{
    //transfer the global settings here
    //use the course dashboard for now
    //transfer out of dashboard folder - should be added on admin level
    public class MasterSettingsManager : IMasterSettingsManager
    {
        private ISchoolEntities _db;
        private string surveyConnString = ConfigParserManager.ConnectionStringReader(ConfigParserManager.ConfigSourcePath(), ConfigSettingConstant.surveyEntitiesKey);
        public MasterSettingsManager()
        {
            string connString = ConfigParserManager.ConnectionStringReader(ConfigParserManager.ConfigSourcePath(), ConfigSettingConstant.schoolEntitiesKey);
            _db = new SchoolEntities(connString);
        }

        public MasterSettingsModel GetMasterSettings() {
            var data = (from m in _db.MasterInfoes
                        orderby m.ID descending
                        select new MasterSettingsModel()
                        {
                            RoomNumberOption = m.RoomNumberOption.HasValue ? m.RoomNumberOption.Value : 0,
                            RoomDirectionsOption = m.RoomDirectionsOption.HasValue ? m.RoomDirectionsOption.Value : 0
                        })
                        .FirstOrDefault();
            return data;
        }

        public List<AudienceModel> GetAudiences()
        {
            return _db.Audiences.Select(a => new AudienceModel()
            {
                AudienceId = a.AudienceID,
                AudienceName = a.Audience1
            }).ToList();
        }

        public List<IconsModel> GetIcons()
        {
            return _db.Icons.Select(i => new IconsModel()
            {
                IconId = i.IconsID,
                IconTitle = i.IconTitle,
                IconImageUrl = i.IconImage
            }).ToList();
        }

        public List<DeparmentModel> GetDepartments()
        {
            return _db.Departments.Select(d => new DeparmentModel()
            {
                DepartmentId = d.DeptID,
                DepartmentName = d.DepartmentName
            }).ToList();
        }

        public List<CourseGroupingModel> GetCourseColorGrouping()
        {
            return _db.CourseCategories.Select(c => new CourseGroupingModel()
            {
                CourseCategoryID = c.CourseCategoryID,
                CourseCategoryColor = c.CourseCategoryColor.ToLower(),
                CourseCategoryName = c.CourseCategoryName,
                UseColorBar = c.UseColorBar
            }).ToList();
        }

        public List<CustomCertificateModel> GetCustomCertificateModel()
        {
            return _db.customcetificates.Select(cc => new Models.School.CustomCertificateModel()
            {
                CustomCertificateId = cc.customcertid,
                CertificateTitle = cc.certtitle
            }).ToList();
        }

        public List<DropdownModel> GetMainCategories()
        {
            return _db.MainCategories
                .Select(mc => new DropdownModel()
                {
                    Value = mc.MainCategory1,
                    DisplayText = mc.MainCategory1
                })
                .Distinct()
                .ToList();
        }

        public List<DropdownModel> GetSubCategories()
        {
            return _db.SubCategories
                .Select(sc => new DropdownModel()
                {
                    Value = sc.SubCategory1,
                    DisplayText = sc.SubCategory1
                })
                .Distinct()
                .ToList();
        }

        public List<DropdownModel> GetSubSubCategories()
        {
            return _db.SubSubCategories
                .Select(ssc => new DropdownModel()
                {
                    Value = ssc.SubSubCategory1,
                    DisplayText = ssc.SubSubCategory1
                })
                .Distinct()
                .ToList();
        }

        public List<PricingOptionsModel> GetAllPricingOptions()
        {
            return _db.PricingOptions.ToList().Select(po => new PricingOptionsModel()
            {
                PricingOptionId = po.PricingOptionID,
                PriceTypeNumber = po.PriceTypeNumber,
                PriceTypeDescription = po.PriceTypedesc,
                Price = po.Price
            })
            .OrderBy(p => p.PricingOptionId)
            .ToList();
        }

        public List<CourseChoices> GetCourseChoices()
        {
            return _db.CourseChoices.Select(cc => new CourseChoices()
            {
                CourseChoiceId = cc.CourseChoiceId,
                CourseChoiceName = cc.CourseChoice1
            })
            .OrderBy(cc => cc.CourseChoiceId)
            .ToList();
        }
        //@TODO: WORK WITH SURVEY ENTITY ASAP
        public List<DropdownModel> GetSurveys()
        {
            using (var db = new SurveyEntities(surveyConnString))
            {
                var surveys = db.Surveys.Where(s => s.SurveyStatus == 1).OrderByDescending(s => s.DefaultSurvey).ThenByDescending(s => s.Name)
                     .Select(s => new DropdownModel()
                     {
                         Value = s.SurveyID.ToString(),
                         DisplayText = s.Name,
                         Extra = s.Description
                     }).ToList();
                return surveys;
            }
        }

        public List<DropdownModel> GetLocations()
        {
            return _db.Locations.ToList()
                .Select(l => new DropdownModel()
                {
                    Value = l.LocationID.ToString(),
                    DisplayText = l.Location1,
                    Extra = Newtonsoft.Json.JsonConvert.SerializeObject(new Location() {
                        LocationID = l.LocationID,
                        Street = l.Street,
                        City = l.City,
                        State = l.State,
                        Zip = l.Zip,
                        country = l.country,
                        LocationURL = l.LocationURL,
                        LocationAdditionalInfo = l.LocationAdditionalInfo
                    }) 
                    // l.Street + "," + l.City + "," + l.State + "," + l.Zip + "," + l.LocationURL + "," + l.LocationAdditionalInfo
                }).ToList();
        }

        public List<DropdownModel> GetRooms()
        {
            return _db.RoomNumbers
                .Select(rn => new DropdownModel()
                {
                    Value = rn.RoomNumber1.ToString(),
                    DisplayText = rn.RoomNumber1
                }).ToList();
        }

        public List<DropdownModel> GetRoomDirections()
        {
            return _db.RoomDirections
                .Select(rd => new DropdownModel()
                {
                    Value = rd.RoomDirectionsId.ToString(),
                    DisplayText = rd.RoomDirectionsTitle
                })
                .ToList();
        }

        public List<DropdownModel> GetCountries() {
            return _db.Countries
                .Where(c => c.disabled != -1)
                .OrderBy(c => c.countryname).ThenBy(c => c.countryorder)
                .Select(c => new DropdownModel()
                {
                    Value = c.countrycode,
                    DisplayText = c.countryname
                })
                .ToList();
        }

        public List<DropdownModel> GetInstructors() {
            return _db.Instructors
                .Select(i => new DropdownModel()
                {
                    Value = i.INSTRUCTORID.ToString(),
                    DisplayText = i.FIRST + " " + i.LAST
                })
                .ToList();
        }

        public List<DropdownModel> GetMaterials() {
            return _db.Materials
                .Select(p => new DropdownModel()
                {
                    Value = p.productID.ToString(),
                    DisplayText = p.product_name,
                    Extra = p.price.Value.ToString()
                })
                .ToList();
        }
    }
}
