$(document).ready(function () {
  $('select[data-filter="role-filter"]').select2({
    placeholder: "Role",
    allowClear: true,
    minimumResultsForSearch: Infinity,
  });

  var table = $("#user_table").DataTable({
    responsive: true,
    pageLength: 10,
    lengthMenu: [
      [10, 25, 50, -1],
      [10, 25, 50, "All"],
    ],
    ordering: true,
    searching: true,
    columnDefs: [
      {
        targets: 1,
      },
      {
        targets: 3,
        orderable: false,
        searchable: false,
      },
    ],
    language: {
      emptyTable: "No users available",
      zeroRecords: "No matching users found",
    },
    drawCallback: function () {
      $('[data-kt-menu-trigger="click"]').each(function () {
        if (!$(this).data("ktMenu")) {
          new KTMenu($(this).get(0));
        }
      });
    },
  });

  $('input[data-filter="search"]').on("keyup", function () {
    table.search(this.value).draw();
  });

  $('select[data-filter="role-filter"]').on("change", function () {
    var userId = $(this).val();
    $.fn.dataTable.ext.search.pop();
    if (userId !== "") {
      $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        var row = $(table.row(dataIndex).node());
        var rowUserId = row.attr("data-accommodation-id");
        return rowUserId === userId;
      });
    }
    table.draw();
  });
});
