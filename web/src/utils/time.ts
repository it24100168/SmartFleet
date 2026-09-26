// API timestamps stay UTC; presentation and datetime-local inputs use IST explicitly.
export function formatIST(value: string): string {
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : value + 'Z';
  return new Intl.DateTimeFormat('en-IN', {timeZone:'Asia/Kolkata', day:'2-digit', month:'short', year:'numeric', hour:'2-digit', minute:'2-digit', second:'2-digit', hour12:true}).format(new Date(utc)) + ' IST';
}
export const istInput = (date: Date) => new Date(date.getTime()+330*60*1000).toISOString().slice(0,16);
export const istToUtc = (value: string) => new Date(value+':00+05:30').toISOString();
