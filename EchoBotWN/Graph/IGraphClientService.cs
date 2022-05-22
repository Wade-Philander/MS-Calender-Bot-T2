using Microsoft.Graph;

namespace EchoBotWN.Graph
{ 
    public interface IGraphClientService
    {
        GraphServiceClient GetAuthenticatedGraphClient(string accessToken);
    }
}