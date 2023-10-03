using AutoMapper;
using CityInfo.API.Entities;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [Route("api/cities/{cityId}/pointsofinterest")]
    [Authorize]
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _localMailService;
        private readonly ICityInfoRepository _repository;
        private readonly IMapper _mapper;

        public PointsOfInterestController(
            ILogger<PointsOfInterestController> logger, 
            IMailService localMailService, 
            ICityInfoRepository repository, 
            IMapper mapper)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _localMailService = localMailService ?? throw new ArgumentNullException(nameof(localMailService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterestForCreationDto>>> GetPointsOfInterest(int cityId)
        {
            //demo purposes
            var cityName = User.Claims.FirstOrDefault(c => c.Type == "city")?.Value;

            //require user city to match the id of the city requested
            if(!await _repository.CityNameMatchesCityIdAsync(cityName, cityId))
            {
                return Forbid();
            }

            try
            {
                if(!await _repository.CityExistsAsync(cityId))
                {
                    _logger.LogInformation(
                        $"City with id {cityId} wasn't found when accessing points of interest.");
                    return NotFound();
                }


                var pointsOfInterest = await _repository.GetPointsOfInterestAsync(cityId);

                return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterest));
            } 
            catch (Exception ex)
            {
                _logger.LogCritical($"Exception while getting points of interest for city with id {cityId}", ex);
                return StatusCode(500, "A problem happened while handling your request.");
            }
        }

        [HttpGet("{pointOfInterestId}", Name ="GetPointOfInterest")]
        public async Task<ActionResult<PointOfInterestForCreationDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {
            if(!await _repository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterest = await _repository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if(pointOfInterest == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest));
        }

        [HttpPost]
        public async Task<ActionResult<PointOfInterestDto>> CreatePointOfInterest(
            int cityId,
            PointOfInterestForCreationDto pointOfInterestDto)
        {
            if(!await _repository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            //map incoming dto to an entity object
            var finalPointOfInterest = _mapper.Map<PointOfInterest>(pointOfInterestDto);

            await _repository.AddPointOfInterestForCityAsync(cityId, finalPointOfInterest);

            //saving an entity will cause it's entity field (ID) to be populated
            await _repository.SaveChangesAsync();

            //map entity back to dto
            var createdPointOfInterestToReturn = _mapper.Map<PointOfInterestDto>(finalPointOfInterest);

            return CreatedAtRoute("GetPointOfInterest", //generates 201
                new
                {
                    cityId = cityId, //explicitly named route variable
                    pointOfInterestId = createdPointOfInterestToReturn.Id
                },
                createdPointOfInterestToReturn);

        }

        [HttpPut("{pointOfInterestId}")]
        public async Task<ActionResult> UpdatePointOfInterest(int cityId, int pointOfInterestId,
            PointOfInterestForUpdateDto pointOfInterest)
        {
            if (!await _repository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _repository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _mapper.Map(pointOfInterest, pointOfInterestEntity);

            await _repository.SaveChangesAsync();

            return NoContent();

        }

        [HttpPatch("{pointofinterestid}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(
            int cityId, int pointOfInterestId,
            JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            if (!await _repository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            //Patch document is of the dto, and not the entity, so we will need to find
            //the entity first, and then map it to a dto before applying the patch
            //document

            var pointOfInterestEntity = await _repository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = 
                _mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);            

            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);

            await _repository.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("{pointofinterestid}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _repository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _repository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _repository.DeletePointOfInterest(pointOfInterestEntity);

            await _repository.SaveChangesAsync();

            _localMailService.Send("Point of interest deleted.",
                $"Point of interest {pointOfInterestEntity.Name} with id {pointOfInterestEntity.Id} was deleted.");
            return NoContent();

        }
    }
}
