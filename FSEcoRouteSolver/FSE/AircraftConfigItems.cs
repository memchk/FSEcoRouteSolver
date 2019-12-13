// <copyright file="AircraftConfigItems.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "AircraftConfigItems", Namespace = "http://server.fseconomy.net")]
    public class AircraftConfigItems : List<AircraftConfig>
    {
    }
}
