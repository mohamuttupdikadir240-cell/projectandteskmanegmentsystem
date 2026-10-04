document.addEventListener("DOMContentLoaded", function () {
    var toggleBtn = document.getElementById("sidebarToggle");
    var sidebar = document.querySelector(".app-sidebar");
    var overlay = document.getElementById("sidebarOverlay");

    function closeSidebar() {
        sidebar.classList.remove("show");
        if (overlay) overlay.classList.remove("show");
    }

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("show");
            if (overlay) overlay.classList.toggle("show");
        });

        if (overlay) {
            overlay.addEventListener("click", closeSidebar);
        }

        sidebar.querySelectorAll("a.nav-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) closeSidebar();
            });
        });
    }

    // Simple client-side table search filter: any input with data-table-search
    document.querySelectorAll("[data-table-search]").forEach(function (input) {
        var targetSelector = input.getAttribute("data-table-search");
        var table = document.querySelector(targetSelector);
        if (!table) return;
        input.addEventListener("keyup", function () {
            var filter = input.value.toLowerCase();
            table.querySelectorAll("tbody tr").forEach(function (row) {
                row.style.display = row.textContent.toLowerCase().indexOf(filter) > -1 ? "" : "none";
            });
        });
    });

    // Auto-dismiss alerts after 5 seconds
    document.querySelectorAll(".alert-auto-dismiss").forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });
});
