using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Events
{
    public sealed class EventKey : EventArgs { }
    public sealed class SomeEventArgs : EventArgs { }
    public sealed class NewMailEventArgs : EventArgs
    {
        private readonly string from;
        private readonly string to;
        private readonly string subject;
        public string From { get => from; }
        public string To { get => to; }
        public string Subject { get => subject; }
        public NewMailEventArgs(string from, string to, string subject)
        {
            this.from = from;
            this.to = to;
            this.subject = subject;
        }
    }

    internal class MailMessage
    {
        public event EventHandler<NewMailEventArgs> NewMail;
        protected virtual void OneNewMessage(NewMailEventArgs e)
        {
            EventHandler<NewMailEventArgs> temp = Volatile.Read(ref NewMail);
            temp?.Invoke(this, e);
        }
        public void GetMessage(string from, string to, string subject)
        {
            NewMailEventArgs e = new NewMailEventArgs(from, to, subject);
            OneNewMessage(e);
        }
    }
    internal sealed class Fax
    {
        public Fax(MailMessage mailMessage) => mailMessage.NewMail += FaxMsg;
        private void FaxMsg(object sender, NewMailEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .Append("From:\t" + e.From)
                .Append("To:\t" + e.To)
                .Append("Subject:\t" + e.Subject);
            Console.WriteLine("Faxing new mail message:");
            Console.WriteLine(stringBuilder.ToString());
        }
        public void Unregiser(MailMessage mailMessage) => mailMessage.NewMail -= FaxMsg;
    }
    

    public sealed class EventSet
    {
        private readonly Dictionary<EventKey, Delegate> events = new Dictionary<EventKey, Delegate>();
        public void Add(EventKey eventKey, Delegate handler)
        {
            Monitor.Enter(events);
            events.TryGetValue(eventKey, out Delegate tempDelegate);
            events[eventKey] = Delegate.Combine(tempDelegate, handler);
            Monitor.Exit(events);
        }
        public void Remove(EventKey eventKey, Delegate handler)
        {
            Monitor.Enter(events);
            bool gotDelegate = events.TryGetValue(eventKey, out Delegate tempDelegate);
            if (gotDelegate == true)
            {
                tempDelegate = Delegate.Remove(tempDelegate, handler);
                if (tempDelegate != null) events[eventKey] = tempDelegate;
                else events.Remove(eventKey);
            }
            Monitor.Exit(events);
        }
        public void Raise(EventKey eventKey, object sender, EventArgs e)
        {
            Monitor.Enter(events);
            events.TryGetValue(eventKey, out Delegate resultDelegate);
            Monitor.Exit(events);
            resultDelegate?.DynamicInvoke(new object[] { sender, e });
        }
    }
    public class TypeWithLotsOfEvents
    {
        protected static readonly EventKey someEventKey = new EventKey();
        protected static readonly EventKey newMailMessageEventKey = new EventKey();

        private readonly EventSet eventSet = new EventSet();
        protected EventSet EventSet { get => eventSet; }

        public event EventHandler<SomeEventArgs> SomeEvents
        {
            add => eventSet.Add(someEventKey, value);
            remove => eventSet.Remove(someEventKey, value);
        }
        public event EventHandler<NewMailEventArgs> MailEvents
        {
            add => eventSet.Add(newMailMessageEventKey, value);
            remove => eventSet.Remove(newMailMessageEventKey, value);
        }
        protected virtual void OnEvent(EventKey eventKey, EventArgs e) => eventSet.Raise(eventKey, this, e);
        public void DoSomethingWithSomeEvent() => OnEvent(someEventKey, new SomeEventArgs());
        public void DoSomethingWithMailEvent() => OnEvent(newMailMessageEventKey, new NewMailEventArgs("Vladyslav", "Vadim", "Work"));
    }

    


    internal class Program
    {
        static void Main(string[] args)
        {
            TypeWithLotsOfEvents lotsOfEvents = new TypeWithLotsOfEvents();
            lotsOfEvents.SomeEvents += HandleSomeEvent;
            lotsOfEvents.MailEvents += HandleMailEvent;

            lotsOfEvents.DoSomethingWithMailEvent();

            lotsOfEvents.MailEvents -= HandleMailEvent;

            lotsOfEvents.DoSomethingWithMailEvent();

            lotsOfEvents.DoSomethingWithSomeEvent();


        }
        public static void HandleMailEvent(object sender, NewMailEventArgs e)
        {
            Console.WriteLine("New mail event handler");
            Console.WriteLine("From:" + e.From);
            Console.WriteLine("To:" + e.To);
            Console.WriteLine("Subject:" + e.Subject);
        }
        public static void HandleSomeEvent(object sender, SomeEventArgs e) => Console.WriteLine("Some event handler");
    }
}
