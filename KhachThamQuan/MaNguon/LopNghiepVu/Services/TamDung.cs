namespace VisitorGuide.Application.Services;

public sealed class TamDung
{
    public ViTriThuyetMinhDto ThucHien(int viTriKyTu, int doDaiNoiDung)
    {
        if (doDaiNoiDung < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doDaiNoiDung));
        }

        return new ViTriThuyetMinhDto(Math.Clamp(viTriKyTu, 0, doDaiNoiDung));
    }
}

public sealed record ViTriThuyetMinhDto(int ViTriKyTu);
