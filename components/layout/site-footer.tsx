export function SiteFooter() {
  return (
    <footer className="border-t border-sand-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 py-6 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
        <p>© {new Date().getFullYear()} Temple. All rights reserved.</p>
        <div className="flex gap-4">
          <a href="/docs/roadmap" className="hover:text-sand-700">
            Roadmap
          </a>
          <a href="/docs/privacy" className="hover:text-sand-700">
            Privacy
          </a>
          <a href="/docs/terms" className="hover:text-sand-700">
            Terms
          </a>
        </div>
      </div>
    </footer>
  );
}
