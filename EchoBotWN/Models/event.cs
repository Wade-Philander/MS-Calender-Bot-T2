namespace EchoBotWN.Models
{
    public class eventModel
    {
        public string id { get; set; }
        public string subject { get; set; }
        public string message { get; set; }
        public string date { get; set; }// ?? this one going to be interesting?

       public eventModel(string id, string subject, string message, string date) {
            this.id = id;
            this.subject = subject;
            this.message = message;
            this.date = date;
        }
    }
}
