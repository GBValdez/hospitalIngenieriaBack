using fletesProyect.Recipe;
using project.utils;

namespace fletesProyect.Dispatch
{
    public class Dispatch:CommonsModel<long>
    {
        public int amount {  get; set; }
        public long recipeId {  get; set; }
        public Recipe.Recipe recipe { get; set; }
    }
}
