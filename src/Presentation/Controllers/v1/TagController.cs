using Application.Common.ActionFilters;
using Application.Common.DTOs.Tags;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.v1;

[Authorize]
[ServiceFilter(typeof(UserExistsAttribute))]
[ServiceFilter(typeof(ValidParameterAttribute))]
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;


    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }
    
    
    [HttpDelete("{guid}")]
    [ServiceFilter(typeof(TagExistsAttribute))]
    public async Task<ActionResult> DeleteTag(string guid)
    {
        await _tagService.DeleteTagAsync(HttpContext.User.Identity!.Name, guid);
        return NoContent();
    }
    
    [HttpPut("{guid}")]
    [ServiceFilter(typeof(TagExistsAttribute))]
    public async Task<ActionResult> UpdateTag(
        string guid, [FromBody] TagForUpdateDto tagUpdateDto)
    {
        await _tagService.UpdateTagAsync(HttpContext.User.Identity!.Name,
                                         guid, tagUpdateDto);
        return StatusCode(201);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagOutDto>>> GetTags()
    {
        var userName = HttpContext.User.Identity!.Name;
        var result = await _tagService.GetTagsAsync(userName);
        return Ok(result);
    }
}