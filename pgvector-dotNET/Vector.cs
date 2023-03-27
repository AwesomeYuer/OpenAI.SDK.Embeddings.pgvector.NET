namespace PgVectors.NET;

public class PgVector
{
    private float[] _pgVector;

    public PgVector(float[] v)
    {
        _pgVector = v;
    }

    public PgVector(string s)
    {
        _pgVector = Array.ConvertAll(s.Substring(1, s.Length - 2).Split(","), v => float.Parse(v));
    }

    public override string ToString()
    {
        return string.Concat("[", string.Join(",", _pgVector), "]");
    }

    public float[] ToArray()
    {
        return _pgVector;
    }
}
