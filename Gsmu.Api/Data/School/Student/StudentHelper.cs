using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data;

namespace Gsmu.Api.Data.School.Student
{
    public static class StudentHelper
    {
        public static void RegisterStudent(Gsmu.Api.Data.School.Entities.Student student, SchoolEntities db = null, bool requireEmail = true)
        {
            if (student.USERNAME == null)
            {
                throw new Exception("The student username must have a value!");
            }
            if (requireEmail && student.EMAIL == null)
            {
                throw new Exception("The student email must have a value!");
            }
            if (student.STUDNUM == null)
            {
                throw new Exception("The student password (studnum) must have a value!");
            }

            student.DISTEMPLOYEE = NewMemberDistemployeeValue;

            var dbWasNull = (db == null);
            if (dbWasNull)
            {
                db = new SchoolEntities();
            }
            db.Students.Add(student);
            if (dbWasNull)
            {
                db.SaveChanges();
            }
        }
        public static void UpdateStudentInfo(Gsmu.Api.Data.School.Entities.Student student)
        {
            int changedValueCount = 0;
            Gsmu.Api.Data.School.Entities.Student studentData = GetStudent(student.STUDENTID);
            using (var db = new SchoolEntities())
            {
                if (studentData.FIRST != student.FIRST && !string.IsNullOrWhiteSpace(student.FIRST))
                {
                    studentData.FIRST = student.FIRST;
                    changedValueCount++;
                }
                if (studentData.LAST != student.LAST && !string.IsNullOrWhiteSpace(student.LAST))
                {
                    studentData.LAST = student.LAST;
                    changedValueCount++;
                }
                if (studentData.EMAIL != student.EMAIL)
                {
                    studentData.EMAIL = student.EMAIL;
                    changedValueCount++;
                }
                if (studentData.ADDRESS != student.ADDRESS)
                {
                    studentData.ADDRESS = student.ADDRESS;
                    changedValueCount++;
                }
                studentData.NOTES = student.NOTES;
                studentData.EXPERIENCE = student.EXPERIENCE;
                studentData.LastUpdateTime = DateTime.Now;
                studentData.date_modified = DateTime.Now;
                if (changedValueCount > 0) {
                    db.Entry(studentData).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        public static Gsmu.Api.Data.School.Entities.Student GetStudent(int studentId)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.STUDENTID == studentId select s).FirstOrDefault();
                return student;
            }
        }
        public static Gsmu.Api.Data.School.Entities.Student GetStudent(string userName, bool fromAuthentication=false)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.USERNAME == userName select s).FirstOrDefault();
                if (fromAuthentication)
                {
                    student.InActive = 0;
                    db.SaveChanges();
                }
                return student;
            }
        }

        public static Gsmu.Api.Data.School.Entities.Student GetStudentByEmail(string currentEmail)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.EMAIL == currentEmail select s).FirstOrDefault();
                return student;
            }
        }

        // this must be exactly same in the v3 version includefunctions.asp getnewmemberdistemployeevalue function
        public static short GetNewMemberDistemployeeValue(int newStudentMemberType)
        {
            switch (newStudentMemberType)
            {
                case 0:
                    return -1;

                case 1:
                    return 0;

                case 2:
                    return -1;

                default:
                    return (short)newStudentMemberType;
            }
        }

        public static short NewMemberDistemployeeValue
        {
            get
            {
                return GetNewMemberDistemployeeValue(
                    Settings.Instance.GetMasterInfo2().DefaultNewStudentMemberType
                );    
            }
        }
    }
}
