using System;
using System.Collections.Generic;

namespace trello.modelos
{

    public partial class Cartoes
    {
        public Options Options { get; set; }
        public List<TrelloCard> Cards { get; set; }
    }

    public partial class TrelloCard
    {
        public string Id { get; set; }
        public Badges Badges { get; set; }
        public List<CheckItemState> CheckItemStates { get; set; }
        public bool? Closed { get; set; }
        public bool? DueComplete { get; set; }
        public DateTimeOffset? DateLastActivity { get; set; }
        public string Desc { get; set; }
        public DescData DescData { get; set; }
        public DateTimeOffset? Due { get; set; }
        public long? DueReminder { get; set; }
        public object Email { get; set; }
        public String? IdBoard { get; set; }
        public List<string> IdChecklists { get; set; }
        public String? IdList { get; set; }
        public List<String> IdMembers { get; set; }
        public List<object> IdMembersVoted { get; set; }
        public long? IdShort { get; set; }
        public string IdAttachmentCover { get; set; }
        public List<Label> Labels { get; set; }
        public List<string> IdLabels { get; set; }
        public bool? ManualCoverAttachment { get; set; }
        public string Name { get; set; }
        public double? Pos { get; set; }
        public string ShortLink { get; set; }
        public Uri ShortUrl { get; set; }
        public DateTimeOffset? Start { get; set; }
        public bool? Subscribed { get; set; }
        public Uri Url { get; set; }
        public Cover Cover { get; set; }
        public bool? IsTemplate { get; set; }
        public object CardRole { get; set; }
    }

    public partial class Badges
    {
        public AttachmentsByType AttachmentsByType { get; set; }
        public bool? Location { get; set; }
        public long? Votes { get; set; }
        public bool? ViewingMemberVoted { get; set; }
        public bool? Subscribed { get; set; }
        public string Fogbugz { get; set; }
        public long? CheckItems { get; set; }
        public long? CheckItemsChecked { get; set; }
        public object CheckItemsEarliestDue { get; set; }
        public long? Comments { get; set; }
        public long? Attachments { get; set; }
        public bool? Description { get; set; }
        public DateTimeOffset? Due { get; set; }
        public bool? DueComplete { get; set; }
        public DateTimeOffset? Start { get; set; }
    }

    public partial class AttachmentsByType
    {
        public Trello Trello { get; set; }
    }

    public partial class Trello
    {
        public long? Board { get; set; }
        public long? Card { get; set; }
    }

    public partial class CheckItemState
    {
        public string IdCheckItem { get; set; }
        public string State { get; set; }
    }

    public partial class Cover
    {
        public string IdAttachment { get; set; }
        public object Color { get; set; }
        public object IdUploadedBackground { get; set; }
        public Size? Size { get; set; }
        public Brightness? Brightness { get; set; }
        public object IdPlugin { get; set; }
        public List<Scaled> Scaled { get; set; }
        public string EdgeColor { get; set; }
    }

    public partial class Scaled
    {
        public string Id { get; set; }
        public string ScaledId { get; set; }
        public bool? ScaledScaled { get; set; }
        public Uri Url { get; set; }
        public long? Bytes { get; set; }
        public long? Height { get; set; }
        public long? Width { get; set; }
    }

    public partial class DescData
    {
        public Emoji Emoji { get; set; }
    }

    public partial class Emoji
    {
    }

    public partial class Label
    {
        public string Id { get; set; }
        public String? IdBoard { get; set; }
        public string Name { get; set; }
        public String? Color { get; set; }
    }

    public partial class Options
    {
        public List<Term> Terms { get; set; }
        public List<object> Modifiers { get; set; }
        public List<string> ModelTypes { get; set; }
        public bool? Partial { get; set; }
    }

    public partial class Term
    {
        public List<string> Fields { get; set; }
        public string Text { get; set; }
        public bool? Partial { get; set; }
    }

    public enum Brightness { Dark, Light };

    public enum Size { Normal };

}
