using System.Collections.Generic;
using System.Linq;

namespace OmniSharp.Models
{
    public class AutoCompleteResponses : IAggregateResponse
    {
        public AutoCompleteResponses(IEnumerable<AutoCompleteResponse> autoCompletes)
        {
            AutoCompletes = autoCompletes;
        }

        public AutoCompleteResponses()
        {
        }

        public IEnumerable<AutoCompleteResponse> AutoCompletes { get; set; }

        public IAggregateResponse Merge(IAggregateResponse response)
        {
            var autoCompleteResponses = (AutoCompleteResponses)response;
            return new AutoCompleteResponses(this.AutoCompletes.Concat(autoCompleteResponses.AutoCompletes));
        }
    }
}