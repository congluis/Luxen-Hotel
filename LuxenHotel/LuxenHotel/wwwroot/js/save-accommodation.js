document.addEventListener("DOMContentLoaded", function () {
  // Register FilePond plugins
  FilePond.registerPlugin(
    FilePondPluginImagePreview,
    FilePondPluginFileValidateType
  );

  // Initialize FilePond
  const pond = FilePond.create(document.getElementById("filepond"), {
    allowMultiple: true,
    maxFiles: 10,
    acceptedFileTypes: ["image/*"],
    labelIdle: `<div class="p-4 d-flex justify-content-between align-items-center">
      <div class="me-5 fs-3x text-primary">
        <i class="fa-solid fa-file-arrow-up"></i>
      </div>
      <div class="text-start">
        <h3 class="fs-5 fw-bold text-dark mb-1">Drop files here or click to upload.</h3>
        <span class="fs-7 text-muted">Upload up to 10 files</span>
      </div>
    </div>`,
    imagePreviewHeight: 170,
  });

  // Get existing media items (if in edit mode)
  const existingMediaContainer = document.getElementById(
    "existing-media-container"
  );
  const existingMediaItems = existingMediaContainer
    ? existingMediaContainer.querySelectorAll("[data-media-url]")
    : [];
  const isEditMode = existingMediaItems.length > 0;

  // Array to track existing media URLs
  const existingMedia = [];
  // Array to track media URLs to delete
  const mediaToDelete = [];

  // In edit mode, load existing images and create tracking inputs
  if (isEditMode) {
    console.log("Edit mode detected, loading existing images");

    // Create hidden input to track media files to delete
    const mediaToDeleteInput = document.createElement("input");
    mediaToDeleteInput.type = "hidden";
    mediaToDeleteInput.id = "mediaToDeleteInput";
    mediaToDeleteInput.name = "MediaToDelete";
    document.querySelector("form").appendChild(mediaToDeleteInput);

    // Process each existing media item
    existingMediaItems.forEach((item) => {
      const mediaUrl = item.getAttribute("data-media-url");
      if (mediaUrl) {
        // Add to tracking array
        existingMedia.push(mediaUrl);

        // Create a file name from the URL
        const fileName = mediaUrl.split("/").pop();

        // Fetch the image and add it to FilePond
        fetch(mediaUrl)
          .then((response) => {
            if (!response.ok) {
              throw new Error(
                `Failed to fetch image: ${response.status} ${response.statusText}`
              );
            }
            return response.blob();
          })
          .then((blob) => {
            // Create a File object
            const file = new File([blob], fileName, {
              type: getFileTypeFromUrl(mediaUrl),
            });

            // Add to FilePond and store the URL reference
            return pond.addFile(file);
          })
          .then((fileItem) => {
            // Store original URL as a custom property on the FilePond file item
            if (fileItem) {
              fileItem.existingUrl = mediaUrl;
            }
          })
          .catch((error) => {
            console.error(`Error loading image ${mediaUrl}:`, error);
          });
      }
    });

    // Initial update of the hidden input for media to delete
    updateMediaToDeleteInput();
  } else {
    console.log("Create mode detected");
  }

  // Function to update the hidden input with media files to delete
  function updateMediaToDeleteInput() {
    const input = document.getElementById("mediaToDeleteInput");
    if (input) {
      input.value = JSON.stringify(mediaToDelete);
    }
  }

  // Function to guess file type from URL
  function getFileTypeFromUrl(url) {
    const extension = url.split(".").pop().toLowerCase();
    switch (extension) {
      case "jpg":
      case "jpeg":
        return "image/jpeg";
      case "png":
        return "image/png";
      case "gif":
        return "image/gif";
      case "webp":
        return "image/webp";
      default:
        return "image/jpeg"; // Default fallback
    }
  }

  // When a file is removed from FilePond
  pond.on("removefile", (error, fileItem) => {
    // Check if it's an existing file (has existingUrl property)
    if (fileItem && fileItem.existingUrl) {
      // Add to the mediaToDelete array
      mediaToDelete.push(fileItem.existingUrl);
      // Remove from tracking array of existing media
      const index = existingMedia.indexOf(fileItem.existingUrl);
      if (index > -1) {
        existingMedia.splice(index, 1);
      }
      updateMediaToDeleteInput();
      console.log(`Marked file for deletion: ${fileItem.existingUrl}`);
    }
  });

  // Handle thumbnail deletion if checkbox is clicked
  const deleteThumbnailCheckbox = document.getElementById("deleteThumbnail");
  if (deleteThumbnailCheckbox) {
    deleteThumbnailCheckbox.addEventListener("change", function () {
      const thumbnailPreview = document.getElementById("thumbnailPreview");
      if (this.checked && thumbnailPreview) {
        thumbnailPreview.classList.add("opacity-50");
      } else if (thumbnailPreview) {
        thumbnailPreview.classList.remove("opacity-50");
      }
    });
  }

  // Handle form submission
  const form = document.querySelector("form");
  form.addEventListener("submit", function (e) {
    // Get all files from FilePond
    const allFiles = pond.getFiles();

    if (allFiles.length === 0) {
      // No files in FilePond, make sure realInput is empty too
      const realInput = document.getElementById("realMediaFiles");
      realInput.value = "";
      // Don't return yet, as we still need to handle media deletion
    }

    // Get the real file input element for new uploads
    const realInput = document.getElementById("realMediaFiles");
    const dataTransfer = new DataTransfer();

    // Count how many new files we find
    let newFileCount = 0;

    // Process all FilePond files
    allFiles.forEach((fileItem) => {
      // Check if this is a new file (doesn't have existingUrl property)
      if (!fileItem.existingUrl) {
        newFileCount++;
        // Add to the real file input
        dataTransfer.items.add(fileItem.file);
      }
    });

    console.log(
      `Processing ${newFileCount} new files, ${existingMedia.length} existing files, and ${mediaToDelete.length} files to delete`
    );

    // Set the files to the real input
    realInput.files = dataTransfer.files;

    // Make sure media to delete input is up to date if in edit mode
    if (isEditMode) {
      updateMediaToDeleteInput();
    }
  });
});

document.addEventListener("DOMContentLoaded", function () {
  // Initialize Quill editor
  const quill = new Quill("#accommodation-desc", {
    theme: "snow",
    placeholder: "Type your content here...",
    modules: {
      toolbar: [
        [{ header: [1, 2, 3, false] }],
        ["bold", "italic", "underline", "strike"],
        [{ color: [] }, { background: [] }],
        [{ list: "ordered" }, { list: "bullet" }],
        ["link", "image"],
        ["clean"],
      ],
    },
  });

  // Reference to the hidden input for storing editor content
  const hiddenInput = document.querySelector("#description-hidden");

  // Function to update hidden input with Quill content
  function updateHiddenInput() {
    hiddenInput.value = quill.root.innerHTML;
  }

  // Handle text changes in Quill
  quill.on("text-change", function () {
    updateHiddenInput();
    console.log("Editor content updated");
  });

  // Load existing content in edit mode
  if (hiddenInput.value) {
    console.log("Edit mode detected, loading existing content");
    quill.root.innerHTML = hiddenInput.value;
  } else {
    console.log("Create mode detected");
  }

  // Handle form submission
  const form = document.querySelector("form");
  if (form) {
    form.addEventListener("submit", function (e) {
      // Ensure hidden input is up-to-date before submission
      updateHiddenInput();
      console.log("Form submitted with editor content");
    });
  }
});

let serviceIndex = 0;

// Function to add a new empty service item
function addServiceItem() {
  const wrapper = document.getElementById("services-wrapper");
  const template = document.getElementById("service-template");
  const clone = template.content.cloneNode(true);
  const index = serviceIndex++;

  // Update unique IDs and attributes for accordion
  const collapseId = `collapseService_${index}`;
  const header = clone.querySelector(".accordion-header");
  const collapse = clone.querySelector(".collapse");

  header.setAttribute("data-bs-target", `#${collapseId}`);
  header.setAttribute("aria-controls", collapseId);
  header.setAttribute("aria-expanded", "true");
  header.classList.remove("collapsed");

  collapse.id = collapseId;
  collapse.classList.add("show");

  // Update input IDs for accessibility
  updateInputIds(clone, index);

  // Update service index display
  clone.querySelector(".service-index").textContent = `Service #${index + 1}`;
  wrapper.appendChild(clone);

  return index;
}

// Helper function to update all input IDs and names
function updateInputIds(element, index) {
  // Update name input
  const nameInput = element.querySelector(`[id^="service_name_"]`);
  nameInput.id = `service_name_${index}`;
  nameInput.name = `Services[${index}].Name`;

  // Update price input
  const priceInput = element.querySelector(`[id^="service_price_"]`);
  priceInput.id = `service_price_${index}`;
  priceInput.name = `Services[${index}].Price`;

  // Update description input
  const descInput = element.querySelector(`[id^="service_description_"]`);
  descInput.id = `service_description_${index}`;
  descInput.name = `Services[${index}].Description`;

  // Update labels
  element
    .querySelector(`label[for^="service_name_"]`)
    .setAttribute("for", `service_name_${index}`);
  element
    .querySelector(`label[for^="service_price_"]`)
    .setAttribute("for", `service_price_${index}`);
  element
    .querySelector(`label[for^="service_description_"]`)
    .setAttribute("for", `service_description_${index}`);

  // Update validation spans if present
  const nameValidation = element.querySelector(
    `[asp-validation-for="Services[0].Name"]`
  );
  if (nameValidation)
    nameValidation.setAttribute(
      "asp-validation-for",
      `Services[${index}].Name`
    );

  const priceValidation = element.querySelector(
    `[asp-validation-for="Services[0].Price"]`
  );
  if (priceValidation)
    priceValidation.setAttribute(
      "asp-validation-for",
      `Services[${index}].Price`
    );

  const descValidation = element.querySelector(
    `[asp-validation-for="Services[0].Description"]`
  );
  if (descValidation)
    descValidation.setAttribute(
      "asp-validation-for",
      `Services[${index}].Description`
    );
}

// Function to remove a service item
function removeServiceItem(button) {
  Swal.fire({
    title: "Confirm",
    text: "Are you sure you want to delete this combo?",
    icon: "warning",
    showCancelButton: true,
    confirmButtonText: "Delete",
    cancelButtonText: "Cancel",
    confirmButtonColor: "#d33",
    cancelButtonColor: "#3085d6",
  }).then((result) => {
    if (result.isConfirmed) {
      const item = button.closest(".service-item");

      // If this is an existing service (has a hidden ID field), add a deletion marker
      const serviceIdInput = item.querySelector("input[name$='.Id']");
      if (serviceIdInput && serviceIdInput.value) {
        const form = document.querySelector("form");
        const hiddenField = document.createElement("input");
        hiddenField.type = "hidden";
        hiddenField.name = "ServicesToDelete[]";
        hiddenField.value = serviceIdInput.value;
        form.appendChild(hiddenField);
      }

      item.remove();
      updateServiceIndices();

      Swal.fire("Deleted!", "Service has been deleted.", "success");
    }
  });
}

// Function to update the service indices (numbers) after deletion
function updateServiceIndices() {
  const serviceItems = document.querySelectorAll(".service-item");
  serviceItems.forEach((item, idx) => {
    item.querySelector(".service-index").textContent = `Service #${idx + 1}`;
  });
}

// Function to add an existing service with values from the model
function addExistingService(service) {
  const index = addServiceItem();

  // Set values from existing service
  const nameField = document.getElementById(`service_name_${index}`);
  const priceField = document.getElementById(`service_price_${index}`);
  const descField = document.getElementById(`service_description_${index}`);

  if (nameField) nameField.value = service.name || "";
  if (priceField) priceField.value = service.price || 0;
  if (descField) descField.value = service.description || "";

  // Add hidden field for service ID to track existing services
  if (service.id) {
    const serviceItem = document
      .querySelector(`#collapseService_${index}`)
      .closest(".service-item");
    const idField = document.createElement("input");
    idField.type = "hidden";
    idField.name = `Services[${index}].Id`;
    idField.value = service.id;
    serviceItem.appendChild(idField);
  }
}

// Function to check if we're in edit mode and have existing services
function loadExistingServices() {
  // Check if window.existingServices exists and is properly populated
  console.log("Checking for existing services:", window.existingServices);

  if (
    window.existingServices &&
    Array.isArray(window.existingServices) &&
    window.existingServices.length > 0
  ) {
    console.log(
      `Found ${window.existingServices.length} existing services to load`
    );

    // Clear any default services first
    document.querySelectorAll(".service-item").forEach((item) => item.remove());

    // Load existing services
    window.existingServices.forEach((service) => {
      console.log("Loading service:", service);
      addExistingService(service);
    });
  } else {
    console.log("No existing services found, adding default empty service");
    // Add one empty service for new accommodations
    addServiceItem();
  }
}

// Initialize when the DOM is fully loaded
window.addEventListener("DOMContentLoaded", () => {
  console.log("DOM loaded, initializing services management");
  loadExistingServices();
});

let comboIndex = 0;

// Function to add a new empty combo item
function addComboItem() {
  const wrapper = document.getElementById("combos-wrapper");
  const template = document.getElementById("combo-template");
  const clone = template.content.cloneNode(true);
  const index = comboIndex++;

  // Update unique IDs and attributes for accordion
  const collapseId = `collapseCombo_${index}`;
  const header = clone.querySelector(".accordion-header");
  const collapse = clone.querySelector(".collapse");

  header.setAttribute("data-bs-target", `#${collapseId}`);
  header.setAttribute("aria-controls", collapseId);
  header.setAttribute("aria-expanded", "true");
  header.classList.remove("collapsed");

  collapse.id = collapseId;
  collapse.classList.add("show");

  // Update input IDs and names
  updateComboInputIds(clone, index);

  // Populate services dropdown
  populateServicesDropdown(clone, index);

  // Update service index display
  clone.querySelector(".combo-index").textContent = `Combo #${index + 1}`;

  // Append to DOM
  wrapper.appendChild(clone);

  // Initialize select2 for the services dropdown
  $(`#combo_services_${index}`)
    .select2({
      placeholder: "Select services",
      allowClear: true,
    })
    .on("change", function () {
      updateEstimatedValue(index);
    });

  // Initialize estimated value
  updateEstimatedValue(index);

  // Focus on the combo name input
  clone.querySelector(`#combo_name_${index}`).focus();

  return index;
}

// Helper function to update all input IDs and names for a combo
function updateComboInputIds(element, index) {
  // Update ID input (if exists)
  const idInput = element.querySelector(`input[name^="Combos"][name$=".Id"]`);
  if (idInput) {
    idInput.name = `Combos[${index}].Id`;
  }

  // Update name input
  const nameInput = element.querySelector(`[id^="combo_name_"]`);
  nameInput.id = `combo_name_${index}`;
  nameInput.name = `Combos[${index}].Name`;

  // Update price input
  const priceInput = element.querySelector(`[id^="combo_price_"]`);
  priceInput.id = `combo_price_${index}`;
  priceInput.name = `Combos[${index}].Price`;

  // Update status select
  const statusSelect = element.querySelector(`[id^="combo_status_"]`);
  statusSelect.id = `combo_status_${index}`;
  statusSelect.name = `Combos[${index}].Status`;

  // Update services select
  const servicesSelect = element.querySelector(`[id^="combo_services_"]`);
  servicesSelect.id = `combo_services_${index}`;
  servicesSelect.name = `Combos[${index}].SelectedServiceIds`;

  // Update estimated value input
  const estimatedValueInput = element.querySelector(
    `[id^="combo_estimated_value_"]`
  );
  estimatedValueInput.id = `combo_estimated_value_${index}`;

  // Update description textarea
  const descInput = element.querySelector(`[id^="combo_description_"]`);
  descInput.id = `combo_description_${index}`;
  descInput.name = `Combos[${index}].Description`;

  // Update labels
  element
    .querySelector(`label[for^="combo_name_"]`)
    .setAttribute("for", `combo_name_${index}`);
  element
    .querySelector(`label[for^="combo_price_"]`)
    .setAttribute("for", `combo_price_${index}`);
  element
    .querySelector(`label[for^="combo_status_"]`)
    .setAttribute("for", `combo_status_${index}`);
  element
    .querySelector(`label[for^="combo_services_"]`)
    .setAttribute("for", `combo_services_${index}`);
  element
    .querySelector(`label[for^="combo_estimated_value_"]`)
    .setAttribute("for", `combo_estimated_value_${index}`);
  element
    .querySelector(`label[for^="combo_description_"]`)
    .setAttribute("for", `combo_description_${index}`);

  // Update validation spans
  updateValidationSpan(element, "Name", index);
  updateValidationSpan(element, "Price", index);
  updateValidationSpan(element, "Status", index);
  updateValidationSpan(element, "SelectedServiceIds", index);
  updateValidationSpan(element, "Description", index);
}

// Helper function to update validation spans
function updateValidationSpan(element, fieldName, index) {
  const validationSpan = element.querySelector(
    `[data-valmsg-for="Combos[0].${fieldName}"]`
  );
  if (validationSpan) {
    validationSpan.setAttribute(
      "data-valmsg-for",
      `Combos[${index}].${fieldName}`
    );
  }
}

// Function to populate the services dropdown
function populateServicesDropdown(element, index) {
  const select = element.querySelector(`#combo_services_${index}`);
  const services = document.querySelectorAll(".service-item");

  services.forEach((service, idx) => {
    const serviceIdInput = service.querySelector("input[name$='.Id']");
    const serviceNameInput = service.querySelector("input[name$='.Name']");
    const servicePriceInput = service.querySelector("input[name$='.Price']");
    const serviceId = serviceIdInput ? serviceIdInput.value : `new_${idx}`;
    const serviceName = serviceNameInput
      ? serviceNameInput.value
      : `Service #${idx + 1}`;
    const servicePrice = servicePriceInput
      ? parseFloat(servicePriceInput.value) || 0
      : 0;

    const option = document.createElement("option");
    option.value = serviceId;
    option.textContent = serviceName || `Service #${idx + 1}`;
    option.dataset.price = servicePrice; // Store price in data attribute
    select.appendChild(option);
  });
}

// Function to calculate and update the estimated value
function updateEstimatedValue(index) {
  const select = document.getElementById(`combo_services_${index}`);
  const estimatedValueInput = document.getElementById(
    `combo_estimated_value_${index}`
  );
  let total = 0;

  if (select) {
    const selectedOptions = Array.from(select.selectedOptions);
    total = selectedOptions.reduce((sum, option) => {
      const price = parseFloat(option.dataset.price) || 0;
      return sum + price;
    }, 0);
  }

  if (estimatedValueInput) {
    estimatedValueInput.value = total.toFixed(0); // No decimals for VND
  }
}

// Function to remove a combo item
function removeComboItem(button) {
  Swal.fire({
    title: "Confirm",
    text: "Are you sure you want to delete this combo?",
    icon: "warning",
    showCancelButton: true,
    confirmButtonText: "Delete",
    cancelButtonText: "Cancel",
    confirmButtonColor: "#d33",
    cancelButtonColor: "#3085d6",
  }).then((result) => {
    if (result.isConfirmed) {
      const item = button.closest(".combo-item");

      // Check if this is an existing combo that needs to be tracked for deletion
      const comboIdInput = item.querySelector("input[name$='.Id']");
      if (comboIdInput && comboIdInput.value) {
        const comboId = comboIdInput.value;
        // Add hidden field to track deletion
        const deleteInput = document.createElement("input");
        deleteInput.type = "hidden";
        deleteInput.name = `CombosToDelete`;
        deleteInput.value = comboId;
        document.querySelector("form").appendChild(deleteInput);
      }

      if (item) {
        item.remove();
        updateComboIndices();
        Swal.fire("Deleted!", "Combo has been deleted.", "success");
      }
    }
  });
}

// Helper function to update combo indices after deletion
function updateComboIndices() {
  const comboItems = document.querySelectorAll(".combo-item");
  comboItems.forEach((item, index) => {
    const header = item.querySelector(".accordion-header");
    const collapse = item.querySelector(".collapse");
    const newCollapseId = `collapseCombo_${index}`;

    // Update header attributes
    header.setAttribute("data-bs-target", `#${newCollapseId}`);
    header.setAttribute("aria-controls", newCollapseId);

    // Update collapse ID
    collapse.id = newCollapseId;

    // Update index title
    const indexTitle = item.querySelector(".combo-index");
    if (indexTitle) {
      indexTitle.textContent = `Combo #${index + 1}`;
    }

    // Update all input IDs and names
    updateComboInputIds(item, index);
  });

  // Reset comboIndex to next available index
  comboIndex = comboItems.length;
}

// Function to add an existing combo with values from the model
function addExistingCombo(combo) {
  const index = addComboItem();

  // Set values from existing combo
  const nameField = document.getElementById(`combo_name_${index}`);
  const priceField = document.getElementById(`combo_price_${index}`);
  const statusField = document.getElementById(`combo_status_${index}`);
  const servicesField = document.getElementById(`combo_services_${index}`);
  const descField = document.getElementById(`combo_description_${index}`);

  if (nameField) nameField.value = combo.name || "";
  if (priceField) priceField.value = combo.price || 0;
  if (statusField) statusField.value = combo.status || 0;
  if (descField) descField.value = combo.description || "";

  // Set selected services
  if (combo.selectedServiceIds && Array.isArray(combo.selectedServiceIds)) {
    combo.selectedServiceIds.forEach((serviceId) => {
      const option = servicesField.querySelector(
        `option[value="${serviceId}"]`
      );
      if (option) option.selected = true;
    });
    $(servicesField).trigger("change"); // Update select2 and estimated value
  }

  // Add hidden field for combo ID
  if (combo.id) {
    const comboItem = document
      .querySelector(`#collapseCombo_${index}`)
      .closest(".combo-item");
    const idField = document.createElement("input");
    idField.type = "hidden";
    idField.name = `Combos[${index}].Id`;
    idField.value = combo.id;
    comboItem.appendChild(idField);
  }
}

// Function to check if we're in edit mode and have existing combos
function load_existingCombos() {
  console.log("Checking for existing combos:", window.existingCombos);

  if (
    window.existingCombos &&
    Array.isArray(window.existingCombos) &&
    window.existingCombos.length > 0
  ) {
    console.log(
      `Found ${window.existingCombos.length} existing combos to load`
    );
    document.querySelectorAll(".combo-item").forEach((item) => item.remove());
    window.existingCombos.forEach((combo) => {
      if (combo && typeof combo === "object") {
        addExistingCombo(combo);
      } else {
        console.warn("Invalid combo data:", combo);
      }
    });
  } else {
    console.log("No existing combos found, adding default empty combo");
    addComboItem();
  }
}

// Initialize when the DOM is fully loaded
window.addEventListener("DOMContentLoaded", () => {
  console.log("DOM loaded, initializing combos management");
  load_existingCombos();

  // Ensure select2 is initialized for any existing select elements
  $("select[name$='.SelectedServiceIds']").each(function () {
    $(this)
      .select2({
        placeholder: "Select services",
        allowClear: true,
      })
      .on("change", function () {
        const index = this.id.replace("combo_services_", "");
        updateEstimatedValue(index);
      });
  });
});
