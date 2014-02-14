using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    /// <summary>
    /// Inspection observation.
    /// </summary>
    public class InspectionObservation
    {
        /// <summary>
        /// Gets or sets the unique ID of
        /// this inspection observation.
        /// </summary>
        public int ObservationID { get; set; }

        /// <summary>
        /// Gets or sets the ARDEX id of the horse
        /// for which this observation is made.
        /// </summary>
        public int HorseID { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the
        /// InspectionSubcategory of this observation.
        /// </summary>
        public int SubcategoryID { get; set; }
        
        /// <summary>
        /// Gets or sets the boolean value for this
        /// observation (useful for the "true/false"
        /// inspection subcategories).
        /// </summary>
        public bool Flag { get; set; }
        
        /// <summary>
        /// Gets or sets the observation text.
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Gets or sets the device time recorded when
        /// this inspection observation was created.
        /// </summary>
        public DateTime Created { get; set; }
        
        /// <summary>
        /// True if the entity has been deleted. Otherwise false.
        /// </summary>
        public bool Expired { get; set; }

        // Added for replication.
        public int OwnerReplicaID { get; set; }
        public Guid EntityGuid { get; set; }
    }
}

