using System.Collections.Generic;

namespace UMP.Services
{
    internal interface IService<T> where T : Video
    {
        T GetVideo(string uri);
        IEnumerable<T> GetAllVideos(string uri);
    }
}
