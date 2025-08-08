namespace Temple.Domain.Identity;

public static class Capability
{
    // Subset initial capabilities – expand later
    public const string OrgManageSettings = "org.manage.settings";
    public const string ScheduleCreateEvent = "schedule.create.event";
    public const string ScheduleRead = "schedule.read";
    public const string ScheduleManageEvent = "schedule.manage.event"; // edit/delete any event
    public const string ChatPostMessage = "chat.post.message";
    public const string ChatPostAnnouncement = "chat.post.announcement";
    public const string DonationViewSummary = "donation.view.summary";
    public const string DonationCreate = "donation.create";
    public const string AutomationManageRules = "automation.manage.rules";
    public const string OrgViewAudit = "org.view.audit";
    public const string NotificationSend = "notification.send";
    public const string ProfileUpdate = "profile.update";
    public const string PlatformManageTenants = "platform.manage.tenants"; // super admin only
    public const string ContentCreateLesson = "content.create.lesson";
    public const string ContentPublishLesson = "content.publish.lesson";
    public const string ChatManageChannel = "chat.manage.channel";
    public const string ChatCreateChannel = "chat.create.channel";
    public const string ChatDeleteChannel = "chat.delete.channel";
    public const string ChatReadPresence = "chat.read.presence";
    public const string MediaUpload = "media.upload";
    public const string PeopleRecordAttendance = "people.record.attendance";
    public const string PeopleAddCareNote = "people.add.carenote";
    public const string PeopleAddPrayerRequest = "people.add.prayer";
    public const string PeopleAddMilestone = "people.add.milestone";
    public const string GroupsRead = "groups.read";
    public const string GroupsManage = "groups.manage";
    public const string GroupsJoin = "groups.join";
    public const string GroupsRecordAttendance = "groups.record.attendance";
    public const string GroupsManageCurriculum = "groups.manage.curriculum";
    public const string GroupsManagePathway = "groups.manage.pathway";
    public const string SongsManage = "songs.manage";
    public const string SongsRead = "songs.read";
    public const string ServicePlansManage = "serviceplans.manage";
    public const string ServicePlansRead = "serviceplans.read";
    public const string SetListsPublish = "setlists.publish";
    public const string VolunteersManagePositions = "volunteers.manage.positions";
    public const string VolunteersAssign = "volunteers.assign";
    public const string VolunteersRecordAvailability = "volunteers.record.availability";
    public const string VolunteersBackgroundCheck = "volunteers.background.check";
    public const string ChildrenCheckIn = "children.checkin";
    public const string ChildrenIncidentReport = "children.incident.report";
    public const string StewardshipManageCampaigns = "stewardship.manage.campaigns";
    public const string StewardshipManageFunds = "stewardship.manage.funds";
    public const string StewardshipRecordNonCash = "stewardship.record.noncash";
    public const string FinanceManageBudget = "finance.manage.budget";
    public const string FinanceSubmitExpense = "finance.submit.expense";
    public const string FinanceApproveExpense = "finance.approve.expense";
    public const string FacilitiesBook = "facilities.book";
    public const string FacilitiesManageBookings = "facilities.manage.bookings";

    public static readonly string[] All =
    [
        OrgManageSettings,
        ScheduleCreateEvent,
        ScheduleRead,
    ScheduleManageEvent,
        ChatPostMessage,
        ChatPostAnnouncement,
        DonationViewSummary,
        DonationCreate,
    AutomationManageRules,
    OrgViewAudit,
    NotificationSend,
    ProfileUpdate,
    PlatformManageTenants,
    ContentCreateLesson,
    ContentPublishLesson,
    ChatManageChannel,
    ChatCreateChannel,
    ChatDeleteChannel,
    ChatReadPresence,
    MediaUpload
    ,PeopleRecordAttendance
    ,PeopleAddCareNote
    ,PeopleAddPrayerRequest
    ,PeopleAddMilestone
    ,GroupsRead
    ,GroupsManage
    ,GroupsJoin
    ,GroupsRecordAttendance
    ,GroupsManageCurriculum
    ,GroupsManagePathway
    ,SongsManage
    ,SongsRead
    ,ServicePlansManage
    ,ServicePlansRead
    ,SetListsPublish
    ,VolunteersManagePositions
    ,VolunteersAssign
    ,VolunteersRecordAvailability
    ,VolunteersBackgroundCheck
    ,ChildrenCheckIn
    ,ChildrenIncidentReport
    ,StewardshipManageCampaigns
    ,StewardshipManageFunds
    ,StewardshipRecordNonCash
    ,FinanceManageBudget
    ,FinanceSubmitExpense
    ,FinanceApproveExpense
    ,FacilitiesBook
    ,FacilitiesManageBookings
    ];
}
