using Microsoft.AspNetCore.Authorization;

namespace BookonnectAPI.Lib;

public class ClaimRequirement: IAuthorizationRequirement
{
	public string ClaimType { get; }

	public ClaimRequirement(string claimType) =>
		ClaimType = claimType;
	
}

