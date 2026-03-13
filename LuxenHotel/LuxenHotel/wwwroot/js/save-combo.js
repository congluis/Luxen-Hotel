$(document).ready(function () {
  $('select[data-filter="accommodation"]').select2({
    placeholder: "Accommodation",
    allowClear: true,
    minimumResultsForSearch: Infinity,
  });

  var table = $("#combo_table").DataTable({
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
      emptyTable: "No combos available",
      zeroRecords: "No matching combos found",
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

  $('select[data-filter="accommodation"]').on("change", function () {
    var accommodationId = $(this).val();
    $.fn.dataTable.ext.search.pop();
    if (accommodationId !== "") {
      $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
        var row = $(table.row(dataIndex).node());
        var rowAccommodationId = row.attr("data-accommodation-id");
        return rowAccommodationId === accommodationId;
      });
    }
    table.draw();
  });

  $("#combo_table").on(
    "click",
    '[data-kt-ecommerce-combo-filter="delete_row"]',
    function (e) {
      e.preventDefault();
      const comboId = $(this).data("combo-id");
      if (confirm("Are you sure you want to delete this combo?")) {
        fetch(`/admin/combos/Delete/${comboId}`, {
          method: "DELETE",
        })
          .then((response) => {
            if (response.ok) {
              table.row($(this).closest("tr")).remove().draw();
              alert("Combo deleted successfully.");
            } else {
              alert("Failed to delete combo.");
            }
          })
          .catch((error) => {
            console.error("Error:", error);
            alert("An error occurred while deleting the combo.");
          });
      }
    }
  );
});

$(document).ready(function () {
  // Handle accommodation selection
  $("#accommodationSelect").on("change", function () {
    const accommodationId = $(this).val();

    $(".services-for-accommodation").hide();
    $(".total-price").text("0");

    if (accommodationId) {
      $("#services_" + accommodationId).show();
      $("#services_placeholder").hide();
      updateTotalPrice(accommodationId);
    } else {
      $("#services_placeholder").show();
    }
  });

  // Handle service checkbox changes
  $(".service-checkbox").on("change", function () {
    const accommodationId = $(this).data("accommodation-id");
    if ($("#services_" + accommodationId).is(":visible")) {
      updateTotalPrice(accommodationId);
    }
  });

  // Reset form and UI when modal is closed
  $("#kt_modal_add_combo").on("hidden.bs.modal", function () {
    $("#kt_modal_add_combo_form")[0].reset();
    $(".services-for-accommodation").hide();
    $(".total-price").text("0"); // Reset all total prices
    $("#services_placeholder").show();
  });

  // Function to calculate and update total price for an accommodation
  function updateTotalPrice(accommodationId) {
    let total = 0;
    $("#services_" + accommodationId)
      .find(".service-checkbox:checked")
      .each(function () {
        total += parseFloat($(this).data("price")) || 0;
      });
    $("#total_price_" + accommodationId).text(total.toLocaleString("en-US"));
  }
});
