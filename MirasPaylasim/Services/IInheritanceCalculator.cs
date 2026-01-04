using System.Collections.Generic;
using MirasPaylasim.Models;

namespace MirasPaylasim.Services
{
    public interface IInheritanceCalculator
    {
        List<Share> Calculate(Calculation input);
    }
}


