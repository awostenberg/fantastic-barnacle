namespace rando;
using FluentAssertions;

/* 
    generate random names
        q: why not use https://github.com/bchavez/Bogus ?
        a: that would get the immediate job done, and quicker. But when we worked this yesterday together it was not clear
           how to do it in the TDD style. Bearing in mind Aristotle's dictum "we build to understand" let's try it.
 
    Begin with an inital test list (TDD in states and moves http://tinyurl.com/bdhk8apt)

    big picture: I imagine 
                1) a provider of random numbers, like rolling a dice.
                   but the System.Random.Next() would be hard to test.
                   So use an interface (IRoll) that I can provide a "loaded dice" implementation, in addition to system.random.
                   This is an example of I in Grenning's TDD guided by zombies. https://blog.wingman-sw.com/tdd-guided-by-zombies

                2) a list of possible names 
                3) a provider of random strings composed from (1) and (2)
                4) a provider of Random<Name> composed from (1),(2) and (3)



    zero
    one                ✅
    many                ✅
    next name

    interface              ✅ 

    using sysran        ✅ 
        
*/


class RandomName {
    readonly string[] _names;
    readonly IRoll _rollDice;       // does the name reveal intent? I am thinking "roll" and "deal" from APL.

    public RandomName(string[] names, IRoll r) {
        _names = names;
        _rollDice = r;
    }

    public string Next() => _names[_rollDice.Next()];

    internal RandomName Skip()
    {
       _ = _rollDice.Next();
       return this;
    }
}

public interface IRoll {
    public int Next();

}

class SequentialLoadedDice:IRoll {
    // loaded dice yield a monotonically increasing sequence (1, 2, 3, ... n) for easier testability; 
    readonly int _nSides;
    int _lastValueRolledZeroBased = 0;
    public SequentialLoadedDice(int _sides) => _nSides= _sides;

    public int Next() {
        int result = _lastValueRolledZeroBased;
        _lastValueRolledZeroBased = _lastValueRolledZeroBased+1<_nSides?_lastValueRolledZeroBased+1:0; //todo rm Skip()
        return result;
    }
}

class SystemRandomDice:IRoll {
    // dice to use in production -- adapting the system psuedo-random number generator 
    private readonly int _nSides;
    private readonly Random _random;

    public SystemRandomDice(int nSides) {
        _nSides = nSides;
        _random = new Random();
        
    }

    public int Next() => _random.Next(0, _nSides);
}


public class RandomNamesTest
{
    private static RandomName DiceFor(params string[] names) {
        RandomName r =  new(names, new SequentialLoadedDice(names.Length));
        return r;
}
    [Fact]
    public void RollOne() =>
        DiceFor("Matthew").Next().Should().Be("Matthew");


    [Fact]
    public void RollTwo() => 
        DiceFor("Matthew", "Mark").Skip().Next().Should().Be("Mark");

    [Fact]
    public void RollThree() => 
        DiceFor("Matthew", "Mark").Skip().Skip().Next().Should().Be("Matthew");


    [Fact]
    public void SysRandom() {
   
        string[] names = {"Matthew","Mark","Luke","John"};
        var sysran = new SystemRandomDice(names.Length);

        var ran = new RandomName(names,sysran);

        names.Should().Contain(ran.Next());

        // ok, how would I have more confidence it worked? 
        // Distribution? No, don't need to test system.random. Just the hook up.
        // That it does not run off end of array? 
        // that all names got picked?  (that latter check revealed an off-by-one mistake so proved a good one)            

        var chosen = new HashSet<string>();
        foreach (var _ in Enumerable.Range(1,100)) {
            chosen.Add(ran.Next());
        }

        chosen.Count.Should().Be(4); 

    }
}