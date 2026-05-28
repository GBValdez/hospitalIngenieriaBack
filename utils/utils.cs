using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using project.utils.dto;

namespace project.utils
{
    public class Utils
    {
        public static bool indexValid<T>(List<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        public async static Task<errClass<resPag<TDto>>> paginate<T, TDto>(IQueryable<T> query, pagQueryDto data, IMapper mapper)
        {
            errClass<resPag<TDto>> res = new errClass<resPag<TDto>>();
            int total = await query.CountAsync();

            if (total == 0)
            {
                res.data = new resPag<TDto>
                {
                    items = new List<TDto>(),
                    total = 0,
                    index = 0,
                    totalPages = 0
                };
                return res;
            }

            int totalPages = (int)Math.Ceiling((double)total / data.pageSize);

            if (data.pageNumber > totalPages && !data.all.Value)
                res.error = new errorMessageDto("El indice de la pagina es mayor que el numero de paginas total");

            if (data.pageNumber < 0 && !data.all.Value)
                res.error = new errorMessageDto("El indice de la pagina no puede ser menor que 0");

            if (res.error != null)
                return res;
            if (data.all == false)
                query = query
                .Skip((data.pageNumber - 1) * data.pageSize)
                .Take(data.pageSize);

            List<T> listDb = await
                query
                .ToListAsync();
            List<TDto> listDto = mapper.Map<List<TDto>>(listDb);
            res.data = new resPag<TDto>
            {
                items = listDto,
                total = total,
                index = data.pageNumber,
                totalPages = totalPages
            };
            return res;
        }

        public static TTEntity CreateDeepCopy<TTEntity>(TTEntity obj) where TTEntity : class
        {
            if (obj == null)
            {
                return null;
            }

            var jsonSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
            };
            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            var serialized = JsonConvert.SerializeObject(obj, jsonSettings);

            return System.Text.Json.JsonSerializer.Deserialize<TTEntity>(serialized);
        }

    }
}