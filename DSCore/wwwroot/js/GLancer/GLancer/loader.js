import {UTFReader} from "./utf.js";

const PROGRESS_BAR_CLASSNAME = "loading-bar",
	PROGRESS_BAR_COMPLETE    = "complete",
	STATUS_CLASSNAME         = "loading-status";

export const status = Object.create(null);

status.IDLE = 0;
status.LOADING = 1;

/**
 * Parallel file load and shared progress monitor
 *
 * @property {HTMLProgressElement} progress       Progress bar
 * @property {HTMLElement}         status         Element to display status text at
 * @property {Array}               queue          Requests queue
 * @property {Number}              queuedCount    Files signed into queue
 * @property {Number}              completedCount Files successfully loaded
 * @property {Number}              pendingCount   Files pending queue
 * @property {Number}              loadingCount   Files currently loading
 */
export class FileLoader {
	constructor(progress = document.createElement("progress"), message = document.createElement("label")) {
		this.path     = new URL("./DATA/", location.origin);
		this.progress = progress;
		this.message  = message;
		this.status   = status.IDLE;
		this.queue    = [];

		this.progress.classList.add(PROGRESS_BAR_CLASSNAME);
		this.message.classList.add(STATUS_CLASSNAME);
	}

	setPath(path) {
		this.path = new URL(path);
	}

	get queuedCount()    { return this.queue.length; }
	get completedCount() { return this.queue.reduce((count, request) => request.readyState == XMLHttpRequest.DONE ? count + 1 : count, 0); }
	get pendingCount()   { return this.queue.reduce((count, request) => request.readyState == XMLHttpRequest.OPENED ? count + 1 : count, 0); }
	get loadingCount()   { return this.queue.reduce((count, request) => request.readyState == XMLHttpRequest.LOADING ? count + 1 : count, 0); }

	/**
	 * Increment progress bar value by amount
	 * @param {Number} amount
	 */
	addProgress(amount) {
		this.progress.value += amount;
	}

	/**
	 * Increment progress bar size (max) by amount. Addition assignment to
	 * progress bar without max attribute causes it first to be initialized to
	 * value of 1 and not 0, because 0 is not a valid value for max attribute.
	 * @param {Number} value
	 */
	addSize(amount) {
		this.progress.max = this.progress.hasAttribute("max") ? this.progress.max + amount : amount;
		this.updateStatus();
	}

	/**
	 * Fired upon completion of a file in queue
	 * @param {String} url Response URL
	 */
	completedFile(url) {
		this.updateStatus();
	}

	/**
	 * Fired upon failing to load a file
	 * @param {String} url  Response URL
	 * @param {String} text Status text
	 */
	failedFile(url, text) {
		this.abort(url + ": " + text);
	}

	/**
	 * Fired upon completion of all queued files
	 */
	completedQueue() {
		this.progress.value = this.progress.max; // Correct value as floating point precision errors will inevetably creep up
		this.progress.classList.toggle(PROGRESS_BAR_COMPLETE, true);
		this.status = status.IDLE;
	}

	/**
	 * Update status text
	 */
	updateStatus() {
		let remainingCount = this.queuedCount - this.completedCount, text;

		// From Quake triggers
		if (remainingCount > 3) text = "There are " + remainingCount + " more to go\u2026";
		else if (remainingCount > 0) text = "Only " + remainingCount + " more to go\u2026";
		else text = "Sequence completed!";

		this.message.textContent = text;
	}

	/**
	 * Fired upon clearing progress for a new batch of files
	 * @param {String} text Message to display
	 */
	clearProgress(text = null) {
		this.queue = [];
		this.progress.classList.toggle(PROGRESS_BAR_COMPLETE, false);
		this.progress.removeAttribute("value");
		this.progress.removeAttribute("max");
		this.message.textContent = text;
	}

	/**
	 * Abort all queued requests, current and pending
	 * @param {String} text Clearing message text
	 */
	abort(text = "Sequence aborted!") {
		this.queue.forEach(request => request.abort());
		this.clearProgress(text);
	}

	/**
	 * Load and split concatenated blob files package by manifest object
	 * @param  {Object} package Package manifest
	 * @return {Map}
	 */
	async loadPackage(manifest) {
		let blob   = await this.load(manifest.url, "blob", manifest.files.reduce((size, file) => size += file.size, 0)),
			offset = 0,
			files  = new Map();
			
		manifest.files.forEach(file => files.set(file.name, blob.slice(offset, offset += file.size, file.type)));
		return files;
	}

	/**
	 * Load text file from URL
	 * @param  {String} url
	 * @return {String}
	 */
	async loadText(url) {
		return await this.load(url, "text", size);
	}

	/**
	 * Load UTF resource from URL
	 * @param  {String} url
	 * @param  {Number} size
	 * @return {UTFReader}
	 */
	async loadUTF(url, size = 1) {
		let blob   = await this.load(url, "blob", size),
			reader = await UTFReader.load(blob);

		return reader;
	}

	/**
	 * Load XML document from URL
	 * @param  {String} url
	 * @param  {Number} size
	 * @return {XMLDocument}
	 */
	async loadXML(url, size = 1) {
		return await this.load(url, "document", size);
	}

	/**
	 * Load file from URL
	 * @param  {String}  url  File URL
	 * @param  {String}  type Response type expected
	 * @param  {Number}  size Relative to other files in queue or some absolute
	 * @return {Promise} Deferred promise (resolved with response, rejected with statusText)
	 */
	load(url, type = "blob", size = 1) {
		if (this.path) url = new URL(url, this.path);

		if (this.completedCount == this.queuedCount) this.clearProgress(); // Clear value & max if loading bar was idle

		let loaded = 0; // Track loaded byte length
		const request = new XMLHttpRequest();

		switch (type) {
			case "text":     request.overrideMimeType("text/plain"); break;
			case "blob":     request.overrideMimeType("application/octet-stream"); break;
			case "document": request.overrideMimeType("application/xml"); break;
		}

		request.open("GET", url, true);
		request.responseType = type;

		// Create executor function for promise
		const executor = (resolve, reject) => {
			const stopQueue = event => {
				this.failedFile(event.target.responseURL, event.target.statusText);
				reject(event.target.statusText);
				return false;
			};

			request.onload = event => {
				if (event.target.status != 200) return stopQueue(event); // Must be HTTP 200

				if (! event.lengthComputable && size > 0) this.addProgress(size); // In case there was no content-length
				this.completedFile(event.target.responseURL);
				if (this.completedCount == this.queuedCount) this.completedQueue(); // Mark if all queued files are complete

				let result = event.target.response;
				if (type == "blob") result = new File([result], url);

				resolve(result);
				return true;
			};

			request.onerror = stopQueue; // Handle error the same as failed download
			request.send();
			this.status = status.LOADING;
		};

		// Track progress as percentage of loaded to total and relative to size
		request.onprogress = event => {
			if (! event.lengthComputable) return false;

			if (size > 0) this.addProgress((event.loaded - loaded) / event.total * size);
			loaded = event.loaded;
		};

		// No point as it gets overriden by last request anyway
		// if (text) request.onloadstart = event => this.message.textContent = text;

		this.queue.push(request);
		if (size > 0) this.addSize(size);
		
		return new Promise(executor);
	}
}

/*
Package manifest object example:

{
	url: "files.dat",
	files:
	[
		{
			name: "basicShader.vs",
			type: "text/plain",
			size: 400
		},
		{
			name: "basicShader.fs",
			type: "text/plain",
			size: 400
		}
	]
}
*/