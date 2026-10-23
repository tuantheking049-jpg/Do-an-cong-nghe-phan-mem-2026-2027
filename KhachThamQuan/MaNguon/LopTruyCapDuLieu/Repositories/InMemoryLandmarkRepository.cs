using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;

namespace VisitorGuide.Infrastructure.Repositories;

public sealed class InMemoryLandmarkRepository : ILandmarkRepository
{
    private static readonly IReadOnlyCollection<Landmark> Landmarks =
    [
        new(
            "tan-hue-vien",
            "Tân Huê Viên",
            "Ẩm thực",
            "Sóc Trăng",
            7,
            4.8,
            320,
            "https://www.dacsanbanhpia.com/wp-content/uploads/2015/05/banh-pia-tan-hue-vien.jpg",
            "https://www.dacsanbanhpia.com/wp-content/uploads/2015/05/banh-pia-tan-hue-vien.jpg",
            "center 55%",
            ["Đặc sản Sóc Trăng", "Bánh pía", "Ẩm thực địa phương"],
            new Dictionary<string, string>
            {
                ["vi"] = "Tân Huê Viên là điểm dừng chân gắn với đặc sản Sóc Trăng, đặc biệt là bánh pía. Ghé thăm để tìm hiểu thêm về món bánh nhiều lớp nhân đậu xanh, sầu riêng và trứng muối, một hương vị quen thuộc của miền Tây.",
                ["en"] = "Tân Huê Viên is a popular stop associated with Sóc Trăng's local specialties, especially bánh pía. Discover this flaky pastry traditionally filled with mung bean, durian and salted egg yolk, a familiar taste of the Mekong Delta.",
                ["fr"] = "Tân Huê Viên est une étape réputée pour les spécialités de Sóc Trăng, notamment le bánh pía. Découvrez cette pâtisserie feuilletée traditionnellement garnie de haricot mungo, de durian et de jaune d'œuf salé."
            }),
        new(
            "cho-noi-nga-nam",
            "Chợ nổi Ngã Năm",
            "Văn hóa",
            "Sóc Trăng",
            8,
            4.8,
            280,
            "https://images.vietnamtourism.gov.vn/vn/images/2014/AnhInternet/chonoinga5.jpg",
            "https://images.vietnamtourism.gov.vn/vn/images/2014/AnhInternet/chonoinga5.jpg",
            "center 50%",
            ["Chợ nổi miền Tây", "Giao thương đường thủy", "Bình minh"],
            new Dictionary<string, string>
            {
                ["vi"] = "Chợ nổi Ngã Năm họp tại nơi năm nhánh sông gặp nhau, tạo nên khung cảnh mua bán tấp nập trên ghe thuyền. Hãy đến từ sáng sớm để cảm nhận nhịp sống sông nước và nét giao thương đặc trưng của miền Tây.",
                ["en"] = "Ngã Năm Floating Market gathers where five waterways meet, with boats bringing local produce and goods to trade. Visit early in the morning to experience the river life and trading traditions of the Mekong Delta.",
                ["fr"] = "Le marché flottant de Ngã Năm se tient au confluent de cinq voies d'eau, où les bateaux viennent échanger produits et marchandises. Venez tôt le matin pour découvrir la vie fluviale du delta du Mékong."
            }),
        new(
            "chua-doi",
            "Chùa Dơi",
            "Kiến trúc",
            "Sóc Trăng",
            8,
            4.8,
            410,
            "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2c/Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_%2840553739771%29.jpg/960px-Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_%2840553739771%29.jpg",
            "https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_(40553739771).jpg",
            "center 55%",
            ["Chùa Khmer", "Kiến trúc Phật giáo", "Vườn cây"],
            new Dictionary<string, string>
            {
                ["vi"] = "Chùa Dơi, còn được biết đến với tên chùa Mahatup, là ngôi chùa Khmer Nam tông nổi tiếng ở Sóc Trăng. Ngôi chùa gây ấn tượng bởi kiến trúc Khmer và khuôn viên nhiều cây xanh, nơi đàn dơi thường trú ngụ trên những tán cây.",
                ["en"] = "Chùa Dơi, also known as Mahatup Pagoda, is a well-known Khmer Theravada Buddhist temple in Sóc Trăng. Its Khmer architecture and leafy grounds are home to colonies of fruit bats roosting in the trees.",
                ["fr"] = "La pagode des Chauves-souris, également appelée Mahatup, est un temple bouddhiste theravāda khmer réputé de Sóc Trăng. Son architecture khmère et ses jardins ombragés accueillent des colonies de chauves-souris."
            }),
        new(
            "chua-phat-hoc-2",
            "Chùa Phật Học 2",
            "Kiến trúc",
            "Sóc Trăng",
            8,
            4.8,
            365,
            "https://upload.wikimedia.org/wikipedia/commons/thumb/5/50/Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg/960px-Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg",
            "https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg",
            "center 50%",
            ["Không gian thanh tịnh", "Kiến trúc Phật giáo", "Vườn tượng"],
            new Dictionary<string, string>
            {
                ["vi"] = "Chùa Phật Học 2 là điểm đến tâm linh được nhiều người ghé thăm tại thành phố Sóc Trăng. Dành chút thời gian dạo quanh khuôn viên rộng rãi, chiêm ngưỡng các công trình Phật giáo và tìm một khoảng lặng giữa hành trình.",
                ["en"] = "Chùa Phật Học 2 is a popular spiritual destination in Sóc Trăng city. Take time to explore its spacious grounds, view the Buddhist structures and enjoy a quiet pause during your visit.",
                ["fr"] = "La pagode Phật Học 2 est un lieu spirituel très visité dans la ville de Sóc Trăng. Prenez le temps de parcourir son vaste espace, d'admirer ses édifices bouddhistes et de profiter d'un moment de calme."
            }),
        new(
            "chua-la-han",
            "Chùa La Hán",
            "Kiến trúc",
            "Sóc Trăng",
            7,
            4.7,
            230,
            "https://upload.wikimedia.org/wikipedia/commons/thumb/5/50/Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg/960px-Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg",
            "https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg",
            "center 58%",
            ["Văn hóa người Hoa", "Kiến trúc chùa", "Không gian thanh tịnh"],
            new Dictionary<string, string>
            {
                ["vi"] = "Chùa La Hán là ngôi chùa gắn với cộng đồng người Hoa ở Sóc Trăng. Ghé thăm để khám phá những chi tiết kiến trúc nhiều màu sắc, không gian thờ tự trang nghiêm và dấu ấn giao thoa văn hóa của vùng đất này.",
                ["en"] = "Chùa La Hán is a temple closely associated with Sóc Trăng's Chinese community. Discover its colorful architectural details, serene worship spaces and the cultural influences that shape this region.",
                ["fr"] = "La pagode La Hán est étroitement liée à la communauté chinoise de Sóc Trăng. Découvrez ses détails architecturaux colorés, ses espaces de culte paisibles et les influences culturelles de la région."
            }),
        new(
            "khu-can-cu-tinh-uy-soc-trang",
            "Khu căn cứ Tỉnh ủy Sóc Trăng",
            "Lịch sử",
            "Sóc Trăng",
            10,
            4.7,
            190,
            "https://media-cdn-v2.laodong.vn/storage/newsportal/2025/4/20/1494178/Soc-Trang-1-01.jpg",
            "https://media-cdn-v2.laodong.vn/storage/newsportal/2025/4/20/1494178/Soc-Trang-1-01.jpg",
            "center 50%",
            ["Di tích lịch sử", "Căn cứ cách mạng", "Rừng Mỹ Phước"],
            new Dictionary<string, string>
            {
                ["vi"] = "Khu căn cứ Tỉnh ủy Sóc Trăng tại rừng Mỹ Phước gắn liền với hoạt động cách mạng của địa phương trong thời kỳ kháng chiến. Tham quan khu di tích giúp bạn tìm hiểu thêm về lịch sử đấu tranh và cuộc sống của cán bộ trong căn cứ.",
                ["en"] = "The Sóc Trăng Provincial Party Committee Base in Mỹ Phước Forest is associated with the local revolutionary movement during the resistance wars. A visit offers insight into the region's history and life at the wartime base.",
                ["fr"] = "La base du Comité provincial du Parti de Sóc Trăng, dans la forêt de Mỹ Phước, est liée au mouvement révolutionnaire local durant les guerres de résistance. La visite permet de découvrir l'histoire de la région et la vie dans cette base."
            })
    ];

    public IReadOnlyCollection<Landmark> GetAll() => Landmarks;

    public Landmark? GetById(string id) =>
        Landmarks.FirstOrDefault(landmark => landmark.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
