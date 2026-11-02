import { forwardRef, useState, type InputHTMLAttributes } from 'react';
import { Eye, EyeOff } from 'lucide-react';
export const Field = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string }
>(function Field({ label, error, type, id, ...props }, ref) {
  const [visible, setVisible] = useState(false);
  const inputId = id ?? props.name;
  return (
    <div className="field">
      <label htmlFor={inputId}>{label}</label>
      <div className={`input-wrap ${type === 'password' ? 'password-input' : ''}`}>
        <input
          {...props}
          ref={ref}
          id={inputId}
          type={type === 'password' && visible ? 'text' : type}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {type === 'password' ? (
          <button
            type="button"
            className="eye"
            onClick={() => setVisible(!visible)}
            aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}
          >
            {visible ? <EyeOff size={19} /> : <Eye size={19} />}
          </button>
        ) : null}
      </div>
      {error ? (
        <span className="field-error" id={`${inputId}-error`}>
          {error}
        </span>
      ) : null}
    </div>
  );
});
