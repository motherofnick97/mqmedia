import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { KolGeneralServiceProxy, KolGeneralDto, KolServiceProxy, KolDto } from '@shared/service-proxies/service-proxies';
import { bankOptions } from '../bank-labels';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';
import { DatePicker } from 'primeng/datepicker';
import moment from 'moment';

@Component({
    templateUrl: './edit-kol-general-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Button,
        Select,
        MultiSelect,
        DatePicker,
    ],
})
export class EditKolGeneralDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    kolGeneral = new KolGeneralDto();
    id: string;
    dob: Date | null = null;
    bankOptions = bankOptions;
    kols: KolDto[] = [];

    constructor(
        injector: Injector,
        public _kolGeneralService: KolGeneralServiceProxy,
        private _kolService: KolServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._kolGeneralService.get(this.id).subscribe((result) => {
            this.kolGeneral = result;
            this.dob = result.dob ? result.dob.toDate() : null;
            this.cd.detectChanges();
        });
        this._kolService.getAll(undefined, undefined, undefined, 'name', 0, 1000).subscribe((r) => {
            this.kols = r.items ?? [];
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this.kolGeneral.dob = this.dob ? moment(this.dob) : undefined;
        this._kolGeneralService.update(this.kolGeneral).subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            () => { this.saving = false; this.cd.detectChanges(); }
        );
    }
}
