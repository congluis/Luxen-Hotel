var EarningsChart = {
    init: function () {
        var e = document.getElementById("earnings_chart");
        if (e) {
            var size = parseInt(e.getAttribute("data-kt-size")) || 70;
            var lineWidth = parseInt(e.getAttribute("data-kt-line")) || 11;

            var canvas = document.createElement("canvas");
            var span = document.createElement("span");

            if (typeof G_vmlCanvasManager !== "undefined") {
                G_vmlCanvasManager.initElement(canvas);
            }

            var ctx = canvas.getContext("2d");
            canvas.width = canvas.height = size;
            e.appendChild(span);
            e.appendChild(canvas);

            // Dữ liệu từ Razor (ViewModel)
            var data = [
                {
                    value: parseFloat(e.getAttribute("data-confirmed")) || 0,
                    color: "#009EF7" // Primary
                },
                {
                    value: parseFloat(e.getAttribute("data-inprogress")) || 0,
                    color: "#50CD89" // Success
                },
                {
                    value: parseFloat(e.getAttribute("data-completed")) || 0,
                    color: "#7239EA" // Info
                }
            ];

            var total = data.reduce((sum, d) => sum + d.value, 0);

            ctx.translate(size / 2, size / 2);
            var radius = (size - lineWidth) / 2;
            var startAngle = -Math.PI / 2;

            data.forEach(function (segment) {
                if (segment.value <= 0) return; // bỏ qua đoạn bằng 0

                var angle = (segment.value / total) * 2 * Math.PI;
                ctx.beginPath();
                ctx.arc(0, 0, radius, startAngle, startAngle + angle, false);
                ctx.strokeStyle = segment.color;
                ctx.lineCap = "round";
                ctx.lineWidth = lineWidth;
                ctx.stroke();
                startAngle += angle;
            });
        }
    }
};

document.addEventListener("DOMContentLoaded", function () {
    EarningsChart.init();
});