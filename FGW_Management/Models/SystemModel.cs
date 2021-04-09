using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace FGW_Management.Models
{
    public enum ContributionStatus { Approved, Pending, Rejected } //for a value permanent.
    public enum FileType { Document , Image }

    public static class _Global
    {

        private static string rootFolderName { get { return "_Files"; } }
        public static string PATH_TOPIC { get { return Path.Combine(rootFolderName, "Topics"); } }
        public static string PATH_TEMP { get { return Path.Combine(rootFolderName, "Temp"); } }
    }
    public class FGW_User : IdentityUser
    {
        public string Number { get; set; }
        [Required(ErrorMessage = "Please enter first name!")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter last name!")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }


        [Required(ErrorMessage = "Please choose gender!")]
        [DisplayName("Gender")]
        public string Gender { get; set; }


        [Required(ErrorMessage = "Please fill your birthday!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? Birthday { get; set; }

        [Required(ErrorMessage = "Please fill your address!")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [DisplayName("Department Name")]
        public int? DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public virtual ICollection<Contribution> Contributions { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }

    }

    public class Department
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter department name!")]
        [DisplayName("Department Name")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Please enter department location!")]
        [DisplayName("Department Location")]
        public string Location { get; set; }

        public virtual ICollection<FGW_User> FGW_Users { get; set; }

    }

    public class Submission
        //Topic
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter Topic name!")]
        [DisplayName("Topic Title")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Please enter Topic creation day!")]
        [DisplayName("Topic Creation Day")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime CreationDay { get; set; }

        [Required(ErrorMessage = "Please enter Topic deadline term 1!")]
        [DisplayName("Topic Deadline term 1")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime SubmissionDeadline_1 { get; set; }

        [Required(ErrorMessage = "Please enter Topic deadline term 2!")]
        [DisplayName("Topic Deadline term 2")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime SubmissionDeadline_2 { get; set; }


        public virtual ICollection<Contribution> Contributions { get; set; }
    }

    public class Contribution
        //contribution
    {

        public int Id { get; set; }
        public ContributionStatus Status { get; set; }


        public string ContributorId { get; set; }
        public virtual FGW_User Contributor { get; set; }
        
        public int SubmissionId { get; set; }
        public virtual Submission Submission { get; set; }


        public virtual ICollection<SubmittedFile> SubmittedFiles { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
    }

    public class SubmittedFile
    {
        public int Id { get; set; }

        [DisplayName("File URL")]
        public string URL { get; set; }

        [DisplayName("FIle Type")]
        public FileType Type { get; set; }


        [DisplayName("Contribution Id")]
        public int ContributionId { get; set; }
        public virtual Contribution Contribution { get; set; }


    }


    public class Comment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        [DisplayName("Comments Detail")]
        public string Content { get; set; }

        [DisplayName("User Name")]
        public string UserId { get; set; }
        public virtual FGW_User User { get; set; }

        public int ContributionId { get; set; }
        public virtual Contribution Contribution { get; set; }
    }

    public class Chat
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        [DisplayName("Chat Content")]
        public string Content { get; set; }

        [DisplayName("User Name")]
        public string UserId { get; set; }
        public virtual FGW_User User { get; set; }

    }

    public class API_Department_Contribution 
    { 
        public string DepartmentName { get; set; }
        public int TotalContribution { get; set; }
    }



}
