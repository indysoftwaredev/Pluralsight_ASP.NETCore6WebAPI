namespace CityInfo.API.Models
{
    /// <summary>
    /// A DTO for a city without points of interest
    /// </summary>
    public class CityWithoutPointsOfInterestDto
    {
        /// <summary>
        /// the id of the city
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// the name of the city
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// the description of the city
        /// </summary>
        public string? Description { get; set; }
    }
}
