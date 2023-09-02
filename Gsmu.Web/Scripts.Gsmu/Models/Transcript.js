﻿Ext.define('Transcript', {
    extend: 'Ext.data.Model',
    idProperty: 'TranscriptID',
    fields: [
        { name: 'TranscriptID', type: 'int' },
        { name: 'STUDENTID', type: 'int' },
        { name: 'StudentsSchool', type: 'string' },
        { name: 'District', type: 'string' },
        { name: 'InstructorName', type: 'string' },
        { name: 'InstructorName2', type: 'string' },
        { name: 'InstructorName3', type: 'string' },
        { name: 'GradeLevel', type: 'string' },
        { name: 'CourseId', type: 'int' },
        { name: 'CourseNum', type: 'string' },
        { name: 'CourseName', type: 'string' },
        { name: 'CourseLocation', type: 'string' },
        { name: 'CourseDate', type: 'string' },
        { name: 'DistPrice', type: 'float' },
        { name: 'NoDistPrice', type: 'float' },
        { name: 'Room', type: 'string' },
        { name: 'Days', type: 'int' },
        { name: 'CreditHours', type: 'float' },
        { name: 'EventNum', type: 'string' },
        { name: 'AccountNum', type: 'string' },
        { name: 'DATEADDED', type: 'date' },
        { name: 'TIMEADDED', type: 'date' },
        { name: 'ATTENDED', type: 'int' },
        { name: 'DIDNTATTEND', type: 'int' },
        { name: 'HOURS', type: 'float' },
        { name: 'CourseCost', type: 'string' },
        { name: 'PAYMETHOD', type: 'string' },
        { name: 'payNumber', type: 'string' },
        { name: 'CardExp', type: 'string' },
        { name: 'AuthNum', type: 'string' },
        { name: 'OrderNumber', type: 'string' },
        { name: 'TotalPaid', type: 'string' },
        { name: 'PaymentNotes', type: 'string' },
        { name: 'ReminderSent', type: 'date' },
        { name: 'PaidInFull', type: 'int' },
        { name: 'Position', type: 'string' },
        { name: 'Job', type: 'string' },
        { name: 'Reminder2Sent', type: 'date' },
        { name: 'StudentGrade', type: 'string' },
        { name: 'PricingOption', type: 'string' },
        { name: 'PricingMember', type: 'int' },
        { name: 'InserviceHours', type: 'float' },
        { name: 'CourseHoursType', type: 'string' },
        { name: 'UserEditedFlag', type: 'int' },
        { name: 'CourseCategoryName', type: 'string' },
        { name: 'CourseCompletionDate', type: 'date' },
        { name: 'CourseStartDate', type: 'date' },
        { name: 'Period', type: 'string' },
        { name: 'LinkedTranscriptID', type: 'int' },
        { name: 'AttendanceDetail', type: 'string' },
        { name: 'CustomCreditHours', type: 'float' },
        { name: 'graduatecredit', type: 'float' },
        { name: 'ceucredit', type: 'float' },
        { name: 'AttendanceStatus', type: 'string' },
        { name: 'OptionalCollectedInfo1', type: 'string' },
        { name: 'RefundedAmount', type: 'int' },
        { name: 'RefundDue', type: 'int' },
        { name: 'Optionalcredithours1', type: 'float' },
        { name: 'onlinecourse', type: 'int' },
        { name: 'datemodified', type: 'date' },
        { name: 'datetranscribed', type: 'date' },
        { name: 'UserAddedFlag', type: 'int' },
        { name: 'DateAutoCertSent', type: 'date' },
        { name: 'districtaddressinfo', type: 'string' },
        { name: 'schooladdressinfo', type: 'string' },
        { name: 'gradeaddressinfo', type: 'string' },
        { name: 'CertificateIssueDate', type: 'date' },
        { name: 'districtaddressinfo2', type: 'string' },
        { name: 'schooladdressinfo2', type: 'string' },
        { name: 'studrosterid', type: 'int' },
        { name: 'IsHoursPaid', type: 'int' },
        { name: 'Optionalcredithours2', type: 'float' },
        { name: 'Optionalcredithours3', type: 'float' },
        { name: 'Optionalcredithours4', type: 'float' },
        { name: 'Optionalcredithours5', type: 'float' },
        { name: 'Optionalcredithours6', type: 'float' },
        { name: 'Optionalcredithours7', type: 'float' },
        { name: 'Optionalcredithours8', type: 'float' }
    ]
});


