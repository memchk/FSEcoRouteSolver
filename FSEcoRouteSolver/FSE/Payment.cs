// <copyright file="Payment.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System;
    using System.Xml.Serialization;

    public sealed class Payment
    {
        public decimal Amount { get; set; }

        public int Id { get; set; }

        [XmlElement(ElementName = "Date")]
        public string DateString
        {
            get
            {
                return this.Date.ToString();
            }

            set
            {
                this.Date = DateTime.Parse(value);
            }
        }

        [XmlIgnore]
        public DateTime Date { get; set; }

        public string To { get; set; }

        public string From { get; set; }
    }
}